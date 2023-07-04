using System.Threading.Channels;

namespace Rowbot.Framework.Blocks
{
    public interface IBlockTarget<TInput> : IBlock
    {
        ChannelReader<TInput[]> Reader { get; }
        ChannelWriter<TInput[]> WriterIn { get; }
    }
}
