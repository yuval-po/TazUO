#nullable enable

using System;
using System.ComponentModel;
using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility.Platforms;
using Microsoft.Xna.Framework;
using Myra.Events;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.Controls.ResizableComponents;

/// <summary>
///     Defines the configurable properties of a <see cref="ResizableWindow" />, such as its
///     resize behavior, whether it can be minimized, and where its size is persisted.
/// </summary>
public class ResizableWindowProps : MyraCommonProps
{
    /// <summary>
    ///     Gets or sets the resize behavior (enabled edges, size limits, and scrollbar mode) of the window.
    /// </summary>
    public ResizeBehavior Resize
    {
        get;
        set
        {
            ResizeBehavior oldValue = field;
            if (SetField(ref field, value))
            {
                oldValue?.PropertyChanged -= OnResizePropertyChanged;
                field?.PropertyChanged += OnResizePropertyChanged;
            }
        }
    } = new();

    /// <summary>
    ///     Gets or sets a value indicating whether the window can be minimized to its title bar.
    /// </summary>
    public bool Minimizable { get; set => SetField(ref field, value); } = true;

    public ControlHelp? Help { get; set => SetField(ref field, value); }

    /// <summary>
    ///     Gets or sets the accessor used to persist and restore the window's size across sessions.
    /// </summary>
    public Accessor<Point?>? InitialSizeStore { get; set => SetField(ref field, value); }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ResizableWindowProps" /> class.
    /// </summary>
    public ResizableWindowProps()
    {
        Resize?.PropertyChanged += OnResizePropertyChanged;
    }

    /// <summary>
    ///     Re-raises property change notifications from <see cref="Resize" /> as a change of the <see cref="Resize" />
    ///     property itself.
    /// </summary>
    /// <param name="sender">The <see cref="ResizeBehavior" /> instance that changed.</param>
    /// <param name="e">Event data describing which property of <see cref="Resize" /> changed.</param>
    private void OnResizePropertyChanged(object? sender, PropertyChangedEventArgs e) => OnPropertyChanged(nameof(Resize));
}

public class ControlHelp : Widget
{
    private Widget _helpButton;

    private readonly string? _helpUrl;
    private readonly Widget? _content;
    private readonly string? _contentModalTitle;

    private InfoModal _helpModal;

    public Func<Point>? ContentModalPlacementGetter { get; set; }

    public ControlHelp(Widget helpButton, Widget content, string? contentModalTitle = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        SetupButtonLayout(helpButton);
        _content = content;
        _contentModalTitle = contentModalTitle;
    }

    public ControlHelp(Widget content, string? contentModalTitle = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        SetupButtonLayout(GetDefaultButtonWidget());
        _content = content;
        _contentModalTitle = contentModalTitle;
    }

    public ControlHelp(string url)
    {
        SetupButtonLayout(GetDefaultButtonWidget());
        ArgumentNullException.ThrowIfNull(url);
        _helpUrl = url;
    }

    private static IconButton GetDefaultButtonWidget() => new("❓", () => { }, TazLang.Get("uicommons_help_button"));

    private void SetupButtonLayout(Widget helpButton)
    {
        ArgumentNullException.ThrowIfNull(helpButton);
        _helpButton = helpButton;
        var layout = new SingleItemLayout<Widget>(this) { Child = _helpButton };
        ChildrenLayout = layout;
    }

    private void ShowHelp()
    {
        if (_content == null)
        {
            if (_helpUrl != null)
                PlatformHelper.LaunchBrowser(_helpUrl);
            return;
        }

        // A dismissed modal is disposed, so a cached reference is only reusable while alive -
        // re-showing a disposed one would do nothing and leave the help button dead after the
        // first dismissal.
        if (_helpModal is { IsDisposed: false })
        {
            if (ContentModalPlacementGetter != null)
            {
                Point pos = ContentModalPlacementGetter();
                _helpModal.SetPosition(pos.X, pos.Y);
            }

            _helpModal.BringOnTop();
            return;
        }

        _helpModal = new InfoModal(_contentModalTitle, _content);
        _helpModal.Show(ContentModalPlacementGetter?.Invoke());
    }


    public override void OnTouchDown(TouchEventArgs args)
    {
        ShowHelp();
        base.OnTouchDown(args);
    }
}
