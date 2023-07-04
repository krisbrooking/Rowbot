namespace Rowbot.Connectors.List
{
    public sealed class ListConnectorOptions<TSource>
    {
        internal List<TSource> Data { get; set; } = new();
        public void AddItem(TSource item) => Data.Add(item);
    }
}
