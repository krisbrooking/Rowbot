using System.Threading.Channels;

namespace Rowbot.Framework.Blocks
{
    public interface IBlockSource<TOutput> : IBlock
    {
        ChannelWriter<TOutput[]>? WriterOut { get; }
        void LinkTo(IBlockTarget<TOutput> target);
    }
}
