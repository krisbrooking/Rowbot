using Rowbot.Framework.Blocks;

namespace Rowbot.Framework.Pipelines.Summary
{
    public interface IBlockSummary
    {
        string Name { get; }
        bool HasCompletedWithoutError { get; }
        int RowsExtracted { get; set; }
        int RowsTransformed { get; set; }
        int RowsInserted { get; set; }
        int RowsUpdated { get; set; }
        int TotalBatches { get; set; }
        Dictionary<string, (Exception Exception, int BatchNumber)> Exceptions { get; set; }
    }

    public sealed class BlockSummary<TBlock> : IBlockSummary
        where TBlock : IBlock
    {
        public string Name => typeof(TBlock).Name;
        public bool HasCompletedWithoutError => Exceptions.Count() == 0;
        public int RowsExtracted { get; set; }
        public int RowsTransformed { get; set; }
        public int RowsInserted { get; set; }
        public int RowsUpdated { get; set; }
        public int TotalBatches { get; set; }
        public Dictionary<string, (Exception Exception, int BatchNumber)> Exceptions { get; set; } = new Dictionary<string, (Exception Exception, int BatchNumber)>();
    }
}
