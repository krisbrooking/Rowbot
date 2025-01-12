using System.Threading.Channels;

namespace Rowbot.Pipelines.Blocks;

public interface IBlockTarget<TInput> : IBlock
{
    ChannelReader<TInput[]> Reader { get; }
    ChannelWriter<TInput[]> WriterIn { get; }
}