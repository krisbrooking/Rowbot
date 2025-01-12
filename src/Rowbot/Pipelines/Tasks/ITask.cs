using Rowbot.Pipelines.Summary;

namespace Rowbot.Pipelines.Tasks;

public interface ITask
{
    bool IsAsync { get; }
    Task<BlockSummary> RunAsync();
    BlockSummary Run();
}