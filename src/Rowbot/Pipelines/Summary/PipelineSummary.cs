namespace Rowbot.Pipelines.Summary;

public sealed class PipelineSummary
{
    public PipelineSummary(
        string pipelineCluster,
        string pipelineContainer,
        string pipelineName,
        List<BlockSummary> blockSummaries,
        TimeSpan runtime)
    {
        Cluster = pipelineCluster;
        Container = pipelineContainer;
        Name = pipelineName;
        BlockSummaries = blockSummaries;
        HasCompletedWithoutError = !blockSummaries.SelectMany(x => x.Exceptions).Any();
        Runtime = runtime;
    }

    public bool HasCompletedWithoutError { get; }
    public string Cluster { get; set; }
    public int Group { get; set; }
    public string Container { get; }
    public string Name { get; }
    public TimeSpan Runtime { get; set; }
    public List<BlockSummary> BlockSummaries { get; private set; }

    public int GetInserts() => BlockSummaries.Sum(x => x.RowsInserted);
    public int GetUpdates() => BlockSummaries.Sum(x => x.RowsUpdated);
    public string GetRuntime() => $"{Runtime.Minutes.ToString("00")}:{Runtime.Seconds.ToString("00")}";
}