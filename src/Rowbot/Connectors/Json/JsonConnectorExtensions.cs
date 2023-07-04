using Rowbot.Connectors.Json;

namespace Rowbot
{
    public static class JsonConnectorExtensions
    {
        /// <summary>
        /// Extracts data from a JSON file.
        /// </summary>
        public static IPipelineExtractor<TSource> ExtractJson<TSource>(this IPipelineBuilder pipelineBuilder, string filePath, Action<JsonReadConnectorOptions<TSource>>? configure = default)
        {
            var options = new JsonReadConnectorOptions<TSource>();
            options.SetFilePath(filePath);
            configure?.Invoke(options);

            return pipelineBuilder.Extract<JsonReadConnector<TSource>, TSource, JsonReadConnectorOptions<TSource>>(options);
        }
    }
}
