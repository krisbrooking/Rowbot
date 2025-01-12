namespace Rowbot.Loaders.Framework;

public sealed class LoadResult<TTarget>(IEnumerable<TTarget> inserts, IEnumerable<RowUpdate<TTarget>> updates)
{
    public IEnumerable<TTarget> Inserts { get; private set; } = inserts;
    public IEnumerable<RowUpdate<TTarget>> Updates { get; private set; } = updates;
}