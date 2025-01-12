namespace Rowbot.Connectors.Csv
{
    public static class CsvConnectorExtensions
    {
        /// <summary>
        /// Extracts data from a CSV file.
        /// </summary>
        public static IExtractBuilderConnectorStep<TInput, TOutput> FromCsv<TInput, TOutput>(
            this IExtractBuilder<TInput, TOutput> builder, 
            string filePath, 
            Action<CsvConnectorOptions<TOutput>>? configure = null)
        {
            var options = new CsvConnectorOptions<TOutput>();
            options.SetFilePath(filePath);
            configure?.Invoke(options);

            return builder.WithConnector<CsvReadConnector<TInput, TOutput>>(connector => connector.Options = options);
        }

        /// <summary>
        /// Loads data to a CSV file.
        /// </summary>
        public static ILoadBuilderConnectorStep<TInput, CsvWriteConnector<TInput>> ToCsv<TInput>(
            this ILoadBuilder<TInput> builder, 
            string filePath, 
            Action<CsvConnectorOptions<TInput>>? configure = null)
        {
            var options = new CsvConnectorOptions<TInput>();
            options.SetFilePath(filePath);
            configure?.Invoke(options);

            return builder.WithConnector<CsvWriteConnector<TInput>>(connector => connector.Options = options);
        }
    }
}
