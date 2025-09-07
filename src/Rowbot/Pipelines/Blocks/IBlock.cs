using Rowbot.Pipelines.Summary;

namespace Rowbot.Pipelines.Blocks;

public interface IBlock
{
    Func<CancellationTokenSource, Task> PrepareTask();
    Action<BlockSummary>? SummaryCallback { get; set; }
}