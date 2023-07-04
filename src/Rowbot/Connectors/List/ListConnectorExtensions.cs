using Rowbot.Connectors.List;

namespace Rowbot
{
    public static class ListConnectorExtensions
    {
        /// <summary>
        /// Extracts data from an in-memory list.
        /// </summary>
        public static IPipelineExtractor<TSource> ExtractList<TSource>(this IPipelineBuilder pipelineBuilder, IEnumerable<TSource> list)
        {
            var options = new ListConnectorOptions<TSource>();
            options.Data = list.ToList();

            return pipelineBuilder.Extract<ListReadConnector<TSource>, TSource, ListConnectorOptions<TSource>>(options);
        }

        /// <summary>
        /// Extracts data from an in-memory list.
        /// </summary>
        public static IPipelineExtractor<TSource> ExtractList<TSource>(this IPipelineBuilder pipelineBuilder, Action<ListConnectorOptions<TSource>> configure)
        {
            var options = new ListConnectorOptions<TSource>();
            configure?.Invoke(options);

            return pipelineBuilder.Extract<ListReadConnector<TSource>, TSource, ListConnectorOptions<TSource>>(options);
        }
    }
}
