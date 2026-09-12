using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers.SpellVisualRange;
using ClassicUO.Game.Managers.Structs;
using ClassicUO.Utility.Logging;
using Lock = System.Threading.Lock;

namespace ClassicUO.Game.Managers;

public sealed partial class AutoUnequipActionManager : IDisposable
{
    private struct Armament(uint serial, Layer layer)
    {
        public readonly uint Serial = serial;
        public readonly Layer Layer = layer;
    }

    /// <summary>
    ///     Carries one batched action's dispatch state out of <see cref="ObjectActionQueue" />.
    ///     The queue fills this in on the main thread as it invokes the action; the consumer thread reads it only
    ///     after <see cref="Dispatched" /> turns true. One instance per tracked action, so a stale action left over
    ///     from an abandoned batch writes to its own object and can never be mistaken for the current one.
    /// </summary>
    private sealed class ActionDispatch
    {
        /// <summary>
        ///     Target cursor instance counter as it read immediately before the action was sent.
        ///     Only meaningful once <see cref="Dispatched" /> is true.
        /// </summary>
        public uint TargetCursorIdAtDispatch;

        /// <summary>
        ///     Set once the action has been invoked. Volatile write publishes <see cref="TargetCursorIdAtDispatch" />.
        /// </summary>
        public volatile bool Dispatched;
    }

    public static AutoUnequipActionManager Instance { get; private set; }

    /// <summary>
    ///     How long to wait for the batch's last action to leave <see cref="ObjectActionQueue" />.
    ///     The queue releases at most one item per <see cref="GlobalActionCooldown" />, and stalls entirely while the
    ///     cursor is holding an item, so this has to tolerate a badly backed-up queue.
    /// </summary>
    private const int BATCH_DISPATCH_TIMEOUT_MS = 30_000;

    /// <summary>
    ///     Headroom added on top of a spell's effective cast time to cover the round trip to the server.
    /// </summary>
    private const int TARGET_CURSOR_LATENCY_SLACK_MS = 1_250;

    /// <summary>
    ///     Floor for the cursor wait, so an instant-cast spell on a laggy link still gets a fair chance.
    /// </summary>
    private const int TARGET_CURSOR_MIN_TIMEOUT_MS = 1_000;

    /// <summary>
    ///     Ceiling for the cursor wait. Past this we would rather re-arm than leave the player disarmed.
    /// </summary>
    private const int TARGET_CURSOR_MAX_TIMEOUT_MS = 10_000;

    /// <summary>
    ///     Cursor wait for an action with no known cast time - a spell missing from
    ///     <see cref="SpellVisualRangeManager" />, or one intercepted before its cache finished loading.
    /// </summary>
    private const int TARGET_CURSOR_DEFAULT_TIMEOUT_MS = 5_000;

    /// <summary>
    ///     How long to wait for an open target cursor to be consumed or canceled before re-arming regardless.
    /// </summary>
    private const int TARGET_CURSOR_CLOSE_TIMEOUT_MS = 10_000;

    private readonly World _world;
    private readonly CancellationTokenSource _cTokenSource = new();
    private readonly Task _interceptConsumerCompletion;

    private readonly Channel<EnqueuedAction> _flushChannel = Channel.CreateUnbounded<EnqueuedAction>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }
    );

    /// <summary>
    ///     Pulsed by <see cref="OnTargetingChanged" /> on every targeting transition. Used purely as a wake-up;
    ///     the waiters always re-read live <see cref="TargetManager" /> state, so a stale or spurious signal only
    ///     costs an extra spin. The wait also supplies the memory barrier that publishes those reads to this thread.
    /// </summary>
    private readonly ManualResetEventSlim _targetWaitHandle = new(false, 0);

    /// <summary>
    ///     Set once the last action of the current batch has been invoked by <see cref="ObjectActionQueue" />.
    /// </summary>
    private readonly ManualResetEventSlim _batchDispatched = new(false, 0);

    #region Dispose

    private readonly Lock _disposalLock = new();
    private bool _disposed;

    #endregion

    #region Accessors

    /// <summary>
    ///     Checks whether the manager is in a valid state and can intercept calls.
    ///     Note that this ignores profile settings; those are per-action and checked by each caller.
    /// </summary>
    /// <returns>True if the manager is ready to intercept, false otherwise</returns>
    private bool CanIntercept => !_disposed && IsPlayerBackpackAvailable;

    /// <summary>
    ///     Whether unequip-on-cast is supported and enabled
    /// </summary>
    private bool IsCastInterceptionEnabled => CanIntercept && ProfileManager.CurrentProfile?.AutoUnequipForCast == true;

    /// <summary>
    ///     Whether unequip-on-potion is supported and enabled
    /// </summary>
    private bool IsPotionInterceptionEnabled => CanIntercept && ProfileManager.CurrentProfile?.AutoUnequipForPotion == true;

    /// <summary>
    ///     Determines whether the player's backpack is available
    /// </summary>
    private bool IsPlayerBackpackAvailable => _world?.Player?.Backpack != null;

    #endregion

    public AutoUnequipActionManager(World world)
    {
        _world = world;
        Instance = this;

        world.TargetManager.TargetingChanged += OnTargetingChanged;

        _interceptConsumerCompletion = Task.Run(ActionConsumer).ContinueWith(result =>
        {
            if (result.IsCanceled || result.Exception == null)
                return;

            Log.Warn("Auto-Unequip manager task processor faulted:");
            Log.Warn(result.Exception.ToString());
            // We could restart, but honestly, if it faulted, we're probably better off leaving it disabled.
            MainThreadQueue.InvokeOnMainThread(() =>
            {
                try
                {
                    GameActions.Print(TazLang.Get("auto_unequip_failed_stopped"), Constants.HUE_ERROR);
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to notify user of auto-equip agent failure: {e}");
                }
            });
        });
    }

    #region Public Methods

    /// <summary>
    ///     Attempts to intercept a spell cast
    /// </summary>
    /// <param name="spellIndex">The spell being intercepted</param>
    /// <returns>True if the spell was intercepted, false otherwise</returns>
    /// <remarks>
    ///     Called on the main thread, which is where the spell's cast time and the player's Faster Casting have to be
    ///     read - the consumer thread must not touch either.
    /// </remarks>
    public bool TryInterceptSpellCast(int spellIndex)
    {
        if (!ShouldInterceptCast(spellIndex))
            return false;

        SpellVisualRangeManager.Instance.TryGetSpellInfo(spellIndex, out SpellRangeInfo spell);

        return _flushChannel.Writer.TryWrite(
            new EnqueuedAction(
                () => GameActions.CastSpellDirect(spellIndex),
                spell?.ExpectTargetCursor ?? false, // Unknowns are assumed to not require targeting
                GetTargetCursorTimeoutMs(spell)
            )
        );
    }

    /// <summary>
    ///     Attempts to intercept a double click
    /// </summary>
    /// <param name="itemSerial">The serial of the item being double-clicked</param>
    /// <param name="sendDoubleClickDelegate">The original 'send double click' function</param>
    /// <returns>True if the click was intercepted, false otherwise</returns>
    public bool TryInterceptDoubleClick(uint itemSerial, Action<uint> sendDoubleClickDelegate) =>
        ShouldInterceptDblClick(itemSerial, sendDoubleClickDelegate) &&
        _flushChannel.Writer.TryWrite(EnqueuedAction.NonTargeting(() => sendDoubleClickDelegate(itemSerial)));

    /// <summary>
    ///     Disposes the manager instance.
    ///     Note this method may take a few milliseconds to return.
    /// </summary>
    public void Dispose()
    {
        lock (_disposalLock)
        {
            if (_disposed)
                return;

            _world.TargetManager.TargetingChanged -= OnTargetingChanged;

            // First, issue a cancel. The token propagates to the wait for the main thread as well
            _cTokenSource.Cancel();

            // Then, close the producers
            _flushChannel.Writer.Complete();

            // Wait for the consumer to return.
            // It owns the wait handles below, so nothing can be blocked on them by the time this returns.
            Task.WaitAll(_interceptConsumerCompletion);
            // Finally, dispose of the rest
            _cTokenSource.Dispose();
            _targetWaitHandle.Dispose();
            _batchDispatched.Dispose();
            Instance = null;

            _disposed = true;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    ///     Determines whether a spell cast should be intercepted
    /// </summary>
    /// <param name="spellIndex">The intercepted spell's index</param>
    /// <returns>True if the spell should be intercepted, false otherwise</returns>
    private bool ShouldInterceptCast(int spellIndex)
    {
        if (!IsCastInterceptionEnabled)
            return false;

        if (spellIndex is >= 100 and <= 678 or >= 700)
            return false;

        List<Armament> arms = GetArmingState();
        if (arms.Count <= 0)
            return false;

        foreach (Armament arm in arms)
        {
            if (!_world.OPL.TryGetNameAndData(arm.Serial, out string _, out string data))
                return true; // If missing from OPL, err on the side of caution and intercept

            if (string.IsNullOrWhiteSpace(data) || !IsSpellChannelling().IsMatch(data))
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Determines whether a Double-Click should be intercepted
    /// </summary>
    /// <param name="serial">The clicked target's serial</param>
    /// <param name="sendDoubleClickDelegate">The original 'send double click' function</param>
    /// <returns>True if the event should be intercepted, false otherwise</returns>
    private bool ShouldInterceptDblClick(uint serial, Action<uint> sendDoubleClickDelegate)
    {
        if (sendDoubleClickDelegate == null || !IsPotionInterceptionEnabled)
            return false;

        return IsDrinkablePotionItem(serial) && GetArmingState().Count > 0;
    }

    /// <summary>
    ///     Works out how long a spell's target cursor may take to show up once the cast has been sent.
    /// </summary>
    /// <param name="spell">The spell's indicator info, or null if it is unknown</param>
    /// <returns>The cursor wait, in milliseconds</returns>
    /// <remarks>
    ///     Main thread only - <see cref="SpellRangeInfo.GetEffectiveCastTime" /> reads the player's Faster Casting
    ///     and skills. The result only has to be the right order of magnitude: it bounds how long the player stays
    ///     disarmed when a cast produces no cursor at all, so a half-second spell should not wait out a six-second one.
    /// </remarks>
    private static int GetTargetCursorTimeoutMs(SpellRangeInfo spell)
    {
        if (spell == null)
            return TARGET_CURSOR_DEFAULT_TIMEOUT_MS;

        double castTimeMs = spell.GetEffectiveCastTime() * 1000;

        return (int)Math.Clamp(
            castTimeMs + TARGET_CURSOR_LATENCY_SLACK_MS,
            TARGET_CURSOR_MIN_TIMEOUT_MS,
            TARGET_CURSOR_MAX_TIMEOUT_MS
        );
    }

    /// <summary>
    ///     Gets a snapshot of the player's current arming state, that is, what weapons/shields they have equipped
    /// </summary>
    /// <returns>A list of equipped weapons/shields</returns>
    private List<Armament> GetArmingState()
    {
        // Check if player has weapons equipped
        Item oneHanded = _world.Player.FindItemByLayer(Layer.OneHanded);
        Item twoHanded = _world.Player.FindItemByLayer(Layer.TwoHanded);

        var arms = new List<Armament>();
        if (oneHanded?.Serial != null)
            arms.Add(new Armament(oneHanded.Serial, Layer.OneHanded));

        if (twoHanded?.Serial != null)
            arms.Add(new Armament(twoHanded.Serial, Layer.TwoHanded));

        return arms;
    }

    /// <summary>
    ///     Heuristically determines whether an item, given by serial, is a player-drinkable potion
    /// </summary>
    /// <param name="serial">The item's serial</param>
    /// <returns></returns>
    private bool IsDrinkablePotionItem(uint serial)
    {
        Item item = _world.Items.Get(serial);
        if (item == null)
            return false;

        // Check if item is 0xF06-0xF09 OR 0xF0B-0xF0C - these are the 'drinkable' potion graphics
        if (item.Graphic is (< 0xF06 or > 0xF09) and (< 0xF0B or > 0xF0C))
            return false;

        // Get the un-localized item name. We perform an extra check here as graphics may be shared by unrelated items
        if (_world.OPL.TryGetNameAndData(item.Serial, out string name, out _))
            // Use a simple regular expression to heuristically determine if something is a potion.
            // To be expanded on with configuration.
            return name != null && IsPotionRegex().IsMatch(name);

        // Data doesn't exist in OPL cache - stay conservative and assume this is *not* a potion
        return false;
    }

    /// <summary>
    ///     The task channel's consumer, responsible for actually performing any disarming/rearming
    /// </summary>
    private async Task ActionConsumer()
    {
        try
        {
            // Wait for interception requests/cancellation
            while (await _flushChannel.Reader.WaitToReadAsync(_cTokenSource.Token))
            {
                // A micro-delay to let producers settle, in case of a series of actions
                await Task.Delay(50, _cTokenSource.Token);

                // Gather whatever tasks we've collected until now
                var tasks = new List<EnqueuedAction>();
                while (_flushChannel.Reader.TryRead(out EnqueuedAction task))
                    tasks.Add(task);

                ExecuteBatchedTasks(tasks);

                // A short delay to avoid excessive spam
                await Task.Delay(200, _cTokenSource.Token);
            }
        }
        catch (OperationCanceledException)
        {
            Log.Info("Auto-Unequip action consumer has been interrupted by a cancellation request");
        }
    }

    /// <summary>
    ///     Executes the given tasks, after ensuring player has been disarmed.
    ///     Re-arms the player, afterward.
    /// </summary>
    /// <param name="tasks">The tasks to execute. These could be "cast a spell" or "drink a potion"</param>
    private void ExecuteBatchedTasks(List<EnqueuedAction> tasks)
    {
        if (_disposed)
            return;

        // Dispatch to MT to avoid any potential sync issues.
        //
        // We need to fetch a 'fresh' state - no point issuing an unequip command if we're not currently armed.
        //
        // Note that there's a slight edge case here - if the equipped armaments were changed
        // after enqueuing a task but before we got here, there may be a change in the 'spell channeling' status.
        //
        // Theoretically, then, a disarm may no longer be necessary.
        // This is, however, a minor and mostly harmless quirk.
        List<Armament> arms = MainThreadQueue.BubblingInvokeOnMainThread(() => IsPlayerBackpackAvailable ? GetArmingState() : null, _cTokenSource.Token);
        if (arms == null) // null means world/player/backpack are gone and we should stop.
            return;

        _cTokenSource.Token.ThrowIfCancellationRequested();

        // Enqueue a disarm if necessary. Any cursor already up would swallow the unequip's item drop,
        // so let it resolve first.
        if (arms.Count > 0)
        {
            _ = WaitCurrentTargetEnd(_cTokenSource.Token);
            EnqueueUnequip(arms);
        }

        _cTokenSource.Token.ThrowIfCancellationRequested();

        // The cursor we re-arm behind is the one raised by the last targeting action in the batch;
        // anything queued after it is fire-and-forget as far as targeting goes.
        int trackedIndex = arms.Count > 0 ? tasks.FindLastIndex(task => task.Targeting) : -1;
        ActionDispatch dispatch = EnqueueActions(tasks, trackedIndex);

        _cTokenSource.Token.ThrowIfCancellationRequested();

        // Enqueue a re-arm if necessary
        if (arms.Count <= 0)
            return;

        if (dispatch != null)
            WaitOutTargetCursor(dispatch, tasks[trackedIndex].TargetCursorTimeoutMs, _cTokenSource.Token);

        EnqueueReEquip(arms);
    }

    /// <summary>
    ///     Enqueues the batch's actions, instrumenting the one at <paramref name="trackedIndex" /> so the consumer
    ///     can tell when it was actually sent and which target cursor it is expected to raise.
    /// </summary>
    /// <param name="tasks">The batched actions to enqueue</param>
    /// <param name="trackedIndex">Index of the action to instrument, or a negative value to instrument none</param>
    /// <returns>The tracked action's dispatch state, or null if nothing was instrumented</returns>
    private ActionDispatch EnqueueActions(List<EnqueuedAction> tasks, int trackedIndex)
    {
        ActionDispatch dispatch = null;

        for (int i = 0; i < tasks.Count; i++)
        {
            Action op = tasks[i].Op;
            ObjectActionQueueItem item;

            if (i != trackedIndex)
                item = new ObjectActionQueueItem(op);
            else
            {
                dispatch = new ActionDispatch();
                item = CreateDispatchTrackedItem(op, dispatch);
            }

            ObjectActionQueue.Instance.Enqueue(item, ActionPriority.EquipItem);
        }

        return dispatch;
    }

    /// <summary>
    ///     Wraps an action so it records the target cursor counter and flags itself as sent.
    /// </summary>
    /// <param name="op">The action to wrap</param>
    /// <param name="dispatch">The state to fill in when the action runs</param>
    /// <returns>The queue item to enqueue</returns>
    private ObjectActionQueueItem CreateDispatchTrackedItem(Action op, ActionDispatch dispatch) =>
        new(
            () =>
            {
                // Sampling here - on the main thread, immediately before the send - keeps the cursor correlation as
                // tight as UO allows. The protocol carries no action-to-target linkage, so a targeting operation
                // interleaving between this line and the server's reply can still fool us.
                dispatch.TargetCursorIdAtDispatch = World.Instance.TargetManager.TargetCursorInstanceId;
                op();
            },
            _ =>
            {
                dispatch.Dispatched = true;
                _batchDispatched.Set();
            }
        );

    /// <summary>
    ///     Waits for the tracked action to be sent, and then for the target cursor it raises to be consumed.
    /// </summary>
    /// <param name="dispatch">The tracked action's dispatch state</param>
    /// <param name="cursorTimeoutMs">How long the cursor may take to appear once the action has been sent</param>
    /// <param name="cToken">Cancels the wait</param>
    /// <exception cref="OperationCanceledException">The manager is shutting down</exception>
    /// <remarks>
    ///     Waiting for the send first matters because <see cref="ObjectActionQueue" /> may sit on the action for
    ///     seconds; timing the cursor from enqueue time would measure the queue, not the cast. Either wait timing out
    ///     simply re-arms early - the re-equip still lands behind the action in the queue either way.
    /// </remarks>
    private void WaitOutTargetCursor(ActionDispatch dispatch, int cursorTimeoutMs, CancellationToken cToken)
    {
        if (!WaitForDispatch(dispatch, cToken))
            return;

        _ = WaitTargetEnd(dispatch.TargetCursorIdAtDispatch, cursorTimeoutMs, cToken);
    }

    /// <summary>
    ///     Blocks until the tracked action has been invoked by <see cref="ObjectActionQueue" />.
    /// </summary>
    /// <param name="dispatch">The tracked action's dispatch state</param>
    /// <param name="cToken">Cancels the wait</param>
    /// <returns>True if the action was sent, false if it did not make it out in time</returns>
    /// <exception cref="OperationCanceledException">The manager is shutting down</exception>
    private bool WaitForDispatch(ActionDispatch dispatch, CancellationToken cToken)
    {
        long deadline = Environment.TickCount64 + BATCH_DISPATCH_TIMEOUT_MS;

        // The signal is shared, so an action left over from an abandoned batch can wake us. Re-checking this
        // batch's own flag turns that into a harmless extra spin.
        while (!dispatch.Dispatched)
        {
            int remainingMs = (int)(deadline - Environment.TickCount64);
            if (remainingMs <= 0 || !_batchDispatched.Wait(remainingMs, cToken))
                return dispatch.Dispatched;

            _batchDispatched.Reset();
        }

        return true;
    }

    private void OnTargetingChanged(object sender, TargetChangedEventArgs e) => _targetWaitHandle.Set();

    /// <summary>
    ///     Waits for the target cursor raised by an action that was sent while the instance counter read
    ///     <paramref name="targCursId" /> to both appear and go away.
    /// </summary>
    /// <param name="targCursId">The cursor instance ID sampled immediately before the action was sent</param>
    /// <param name="cursorTimeoutMs">
    ///     How long the cursor may take to appear, per <see cref="GetTargetCursorTimeoutMs" />
    /// </param>
    /// <param name="cToken">Cancels the wait</param>
    /// <returns>True if the cursor came and went, false if either wait timed out</returns>
    /// <exception cref="OperationCanceledException">The manager is shutting down</exception>
    private bool WaitTargetEnd(uint targCursId, int cursorTimeoutMs, CancellationToken cToken)
    {
        // The counter advances on every transition, so it reads targCursId + 1 once our cursor is up and
        // targCursId + 2 once it has closed. Waiting for it to merely pass targCursId would return on open.
        while (World.Instance.TargetManager.TargetCursorInstanceId <= targCursId)
            if (!WaitTargetingTransition(cursorTimeoutMs, cToken))
                return false;

        // The cursor may have already been consumed by the time we got here, in which case this returns at once.
        return WaitCurrentTargetEnd(cToken);
    }

    /// <summary>
    ///     Waits for the currently open target cursor, if any, to be consumed or cancelled.
    /// </summary>
    /// <param name="cToken">Cancels the wait</param>
    /// <returns>True if no cursor is open on return, false if the wait timed out</returns>
    /// <exception cref="OperationCanceledException">The manager is shutting down</exception>
    private bool WaitCurrentTargetEnd(CancellationToken cToken)
    {
        while (World.Instance.TargetManager.IsTargeting)
            if (!WaitTargetingTransition(TARGET_CURSOR_CLOSE_TIMEOUT_MS, cToken))
                return false;

        return true;
    }

    /// <summary>
    ///     Blocks until targeting state changes or the timeout elapses.
    /// </summary>
    /// <param name="milliSecondsTimeout">How long to wait for a transition</param>
    /// <param name="cToken">Cancels the wait</param>
    /// <returns>True if a transition was signalled, false on timeout</returns>
    /// <exception cref="OperationCanceledException">The manager is shutting down</exception>
    private bool WaitTargetingTransition(int milliSecondsTimeout, CancellationToken cToken)
    {
        // Emulating a lightweight AutoResetEvent here (no KObject).
        // Resetting after the wait can swallow a signal raised in between, but every caller re-reads live
        // targeting state afterwards, and that read already reflects the transition the signal announced.
        bool res = _targetWaitHandle.Wait(milliSecondsTimeout, cToken);
        _targetWaitHandle.Reset();
        return res;
    }

    /// <summary>
    ///     Enqueues un-equip actions, if the player is currently armed
    /// </summary>
    /// <returns></returns>
    private void EnqueueUnequip(IList<Armament> arms)
    {
        // Issue an unequip for each equipped armament.
        // Future compatability with Cephalopod-based players.
        foreach (Armament arm in arms ?? [])
            ObjectActionQueue.Instance.Enqueue(
                CreateUnequipQueueItem(arm.Serial, arm.Layer),
                ActionPriority.EquipItem
            );
    }

    /// <summary>
    ///     Create an un-equip queue item, to be used with the object queue
    /// </summary>
    /// <param name="serial">The item to un-equip</param>
    /// <param name="layer">The item's layer, used for validation</param>
    /// <returns></returns>
    private ObjectActionQueueItem CreateUnequipQueueItem(uint serial, Layer layer) =>
        new(() =>
        {
            if (!IsPlayerBackpackAvailable)
                return;

            Item item = _world.Items?.Get(serial);
            if (item == null || item.Container != _world.Player.Serial || item.Layer != layer)
                return;

            new MoveRequest(serial, _world.Player.Backpack.Serial, item.Amount).Execute();
        });

    /// <summary>
    ///     Enqueues the given armaments for re-equipment
    /// </summary>
    /// <param name="arms">The armaments to re-equip</param>
    private void EnqueueReEquip(IList<Armament> arms)
    {
        foreach (Armament arm in arms)
            ObjectActionQueue.Instance.Enqueue(new ObjectActionQueueItem(() =>
            {
                Item item = _world?.Items?.Get(arm.Serial);
                Item backpackItem = _world?.Player?.Backpack;

                if (item != null && backpackItem != null && item.Container == backpackItem.Serial)
                    MoveRequest.EquipItem(arm.Serial, arm.Layer)?.Execute();
            }), ActionPriority.EquipItem);
    }

    #endregion

    #region Regular Expressions

    /// <summary>
    ///     A regex used to match an item's `Spell Channeling` property
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex(@"^\s*Spell Channeling\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex IsSpellChannelling();

    /// <summary>
    ///     A regex used to match standard 'drinkable' potion names
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex(@"(Strength|Agility|Heal|Cure|Nightsight|Refresh(ment)?)\s+Potion", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex IsPotionRegex();

    #endregion

    /// <summary>
    ///     One intercepted action, plus what the consumer needs to know about the target cursor it may raise.
    /// </summary>
    /// <param name="Op">The original action, deferred until the player has been disarmed</param>
    /// <param name="Targeting">Whether the action is expected to raise a target cursor</param>
    /// <param name="TargetCursorTimeoutMs">
    ///     How long that cursor may take to appear once the action is sent. Ignored when <paramref name="Targeting" />
    ///     is false.
    /// </param>
    private record struct EnqueuedAction(Action Op, bool Targeting, int TargetCursorTimeoutMs)
    {
        /// <summary>
        ///     Creates an action that is not expected to raise a target cursor.
        /// </summary>
        /// <param name="op">The original action</param>
        /// <returns>The wrapped action</returns>
        public static EnqueuedAction NonTargeting(Action op) => new(op, false, 0);
    }
}
