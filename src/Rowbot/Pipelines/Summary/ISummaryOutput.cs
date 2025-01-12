namespace Rowbot.Pipelines.Summary;

public interface ISummaryOutput
{
    Task<bool> OutputAsync(IEnumerable<PipelineSummary> pipelineSummaries);
}