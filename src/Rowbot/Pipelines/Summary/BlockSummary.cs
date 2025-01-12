using Rowbot.Pipelines.Blocks;

namespace Rowbot.Pipelines.Summary;

public sealed class BlockSummary(string name)
{
    public string Name => name;
    public bool HasCompletedWithoutError => Exceptions.Count() == 0;
    public int RowsExtracted { get; set; }
    public int RowsTransformed { get; set; }
    public int RowsInserted { get; set; }
    public int RowsUpdated { get; set; }
    public int TotalBatches { get; set; }
    public Dictionary<string, (Exception Exception, int BatchNumber)> Exceptions { get; set; } = new();
}

public static class BlockSummaryFactory
{
    public static BlockSummary Create<TBlock>()
        where TBlock : IBlock
    {
        return new BlockSummary(typeof(TBlock).Name);
    }
}