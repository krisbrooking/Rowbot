namespace Rowbot.Connectors.List
{
    public sealed class ListReadConnector<TSource> : IReadConnector<TSource, ListConnectorOptions<TSource>>
    {
        public ListReadConnector()
        {
            Options = new();
        }

        public ListConnectorOptions<TSource> Options { get; set; }

        public Task<IEnumerable<TSource>> QueryAsync(IEnumerable<ExtractParameter> parameters)
        {
            return Task.FromResult(Options.Data.AsEnumerable());
        }
    }
}
