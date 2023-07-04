namespace Rowbot.Framework.Blocks.Loaders
{
    public sealed class LoadResult<TTarget>
    {
        public LoadResult(IEnumerable<TTarget> inserts, IEnumerable<Update<TTarget>> updates)
        {
            Inserts = inserts;
            Updates = updates;
        }

        public IEnumerable<TTarget> Inserts { get; private set; }
        public IEnumerable<Update<TTarget>> Updates { get; private set; }
    }
}
