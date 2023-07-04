using Rowbot.Framework.Pipelines.Options;
using Rowbot.Framework.Pipelines.Summary;

namespace Rowbot.Framework.Blocks
{
    public sealed class BlockContext
    {
        public Action<IBlockSummary>? SummaryCallback { get; set; }
        public ExtractOptions ExtractOptions { get; set; } = new();
        public TransformOptions TransformerOptions { get; set; } = new();
        public LoadOptions LoadOptions { get; set; } = new();
    }
}
