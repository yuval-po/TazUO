using System.Threading.Channels;

namespace ClassicUO.Ipc;

public interface IIpcConnector {}

public interface IIpcHost<TSend, TReceive>
{
    ChannelWriter<TSend> Send { get; }
    ChannelReader<TReceive> Receive { get; }
}
