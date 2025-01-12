using Rowbot.Pipelines.Summary;

namespace Rowbot.Pipelines.Blocks;

public interface IBlock
{
    Func<Task> PrepareTask();
    Action<BlockSummary>? SummaryCallback { get; set; }
}