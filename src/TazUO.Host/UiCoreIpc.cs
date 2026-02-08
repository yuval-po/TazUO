using System;
using System.Threading.Channels;
using ClassicUO.Ipc;

namespace TazUO.Host;

public class UiCoreIpc
{
    #region Members

    private readonly Channel<ICoreToUiMessage> _coreToUi = Channel.CreateUnbounded<ICoreToUiMessage>();
    private readonly Channel<IUiToCoreMessage> _uiToCore = Channel.CreateUnbounded<IUiToCoreMessage>();

    #endregion

    #region Accessors

    public IIpcHost<ICoreToUiMessage, IUiToCoreMessage> Core { get; }
    public IIpcHost<IUiToCoreMessage, ICoreToUiMessage> Ui { get; }

    #endregion

    public UiCoreIpc()
    {
        Core = new IpcChannelHolder<ICoreToUiMessage, IUiToCoreMessage>(_coreToUi.Writer, _uiToCore.Reader);
        Ui = new IpcChannelHolder<IUiToCoreMessage, ICoreToUiMessage>(_uiToCore.Writer, _coreToUi.Reader);
    }

    private class IpcChannelHolder<TSend, TReceive> : IIpcHost<TSend, TReceive>
    {
        public ChannelWriter<TSend> Send { get; }
        public ChannelReader<TReceive> Receive { get; }

        public IpcChannelHolder(ChannelWriter<TSend> send, ChannelReader<TReceive> receive)
        {
            ArgumentNullException.ThrowIfNull(send);
            ArgumentNullException.ThrowIfNull(receive);
            Send = send;
            Receive = receive;
        }
    }
}
