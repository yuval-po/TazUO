using System.Threading.Channels;

namespace ClassicUO.Ipc;


public class UiCoreIpc
{
    #region Members

    private readonly Channel<ICoreToUiMessage> _coreToUi = Channel.CreateUnbounded<ICoreToUiMessage>();
    private readonly Channel<IUiToCoreMessage> _uiToCore = Channel.CreateUnbounded<IUiToCoreMessage>();

    #endregion

    #region Accessors

    /// <summary>
    ///     Used by the core to send messages to the Avalonia UI
    /// </summary>
    public ChannelWriter<ICoreToUiMessage> ToUi => _coreToUi.Writer;

    /// <summary>
    ///     Used by the core to consume messages from Avalonia UI
    /// </summary>
    public ChannelReader<IUiToCoreMessage> CoreConsumer => _uiToCore.Reader;

    /// <summary>
    ///     Used by the Avalonia host to read messages from the core
    /// </summary>
    internal ChannelReader<ICoreToUiMessage> UiConsumer => _coreToUi.Reader;

    /// <summary>
    ///     Used by the Avalonia host to send messages back to the core
    /// </summary>
    internal ChannelWriter<IUiToCoreMessage> ToCore => _uiToCore.Writer;

    #endregion
}
