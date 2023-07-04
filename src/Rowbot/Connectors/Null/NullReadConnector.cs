namespace Rowbot.Connectors.Null
{
    /// <summary>
    /// NullReadConnector can be used as a placeholder for <see cref="IExtractor{TSource, TOptions}.Connector"/> in the constructor of a custom extractor.
    /// </summary>
    public sealed class NullReadConnector<TSource> : IReadConnector<TSource, NullConnectorOptions>
    {
        public NullReadConnector()
        {
            Options = new();
        }

        public NullConnectorOptions Options { get; set; }

        public Task<IEnumerable<TSource>> QueryAsync(IEnumerable<ExtractParameter> parameters) 
            => Task.FromResult(Enumerable.Empty<TSource>());
    }
}
