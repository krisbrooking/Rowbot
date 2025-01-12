using System.Threading.Channels;

namespace Rowbot.Pipelines.Blocks;

public interface IBlockSource<TOutput> : IBlock
{
    ChannelWriter<TOutput[]>? WriterOut { get; }
    void LinkTo(IBlockTarget<TOutput> target);
}