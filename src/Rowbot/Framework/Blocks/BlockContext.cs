using Rowbot.Framework.Pipelines.Options;
using Rowbot.Framework.Pipelines.Summary;

namespace Rowbot.Framework.Blocks
{
    public sealed class BlockContext
    {
        public Action<IBlockSummary>? SummaryCallback { get; set; }
        public ExtractorOptions ExtractorOptions { get; set; } = new();
        public TransformerOptions TransformerOptions { get; set; } = new();
        public LoaderOptions LoaderOptions { get; set; } = new();
    }
}
