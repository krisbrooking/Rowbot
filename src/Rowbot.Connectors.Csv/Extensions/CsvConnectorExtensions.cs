using Rowbot.Connectors.Csv;

namespace Rowbot
{
    public static class CsvConnectorExtensions
    {
        /// <summary>
        /// Extracts data from a CSV file.
        /// </summary>
        public static IPipelineExtractor<TSource> ExtractCsv<TSource>(this IPipelineBuilder pipelineBuilder, string filePath, Action<CsvConnectorOptions<TSource>>? configure = default)
        {
            var options = new CsvConnectorOptions<TSource>();
            options.SetFilePath(filePath);
            configure?.Invoke(options);

            return pipelineBuilder.Extract<CsvReadConnector<TSource>, TSource, CsvConnectorOptions<TSource>>(options);
        }

        /// <summary>
        /// Loads data to a CSV file.
        /// </summary>
        public static IPipelineLoader<TTarget> LoadCsv<TSource, TTarget>(this IPipelineTransformer<TSource, TTarget> pipelineTransformer, string filePath, Action<CsvConnectorOptions<TTarget>>? configure = default)
        {
            var options = new CsvConnectorOptions<TTarget>();
            options.SetFilePath(filePath);
            configure?.Invoke(options);

            return pipelineTransformer.Load<CsvWriteConnector<TTarget>, TTarget, CsvConnectorOptions<TTarget>>(options);
        }
    }
}
