namespace Rowbot.UnitTests.Connectors.DataTable
{
    public static class DataTableConnectorExtensions
    {
        public static IExtractBuilderConnectorStep<TInput, TOutput> ExtractDataTable<TInput, TOutput>(
            this IExtractBuilder<TInput, TOutput> builder,
            Action<DataTableConnectorOptions<TOutput>>? configure = null)
        {
            var options = new DataTableConnectorOptions<TOutput>();
            configure?.Invoke(options);

            return builder.WithConnector<DataTableReadConnector<TInput, TOutput>>(extractor => extractor.Options = options);
        }

        public static ILoadBuilderConnectorStep<TInput, DataTableWriteConnector<TInput>> LoadDataTable<TInput>(
            this ILoadBuilder<TInput> builder,
            Action<DataTableConnectorOptions<TInput>>? configure = null)
        {
            var options = new DataTableConnectorOptions<TInput>();
            configure?.Invoke(options);

            return builder.WithConnector<DataTableWriteConnector<TInput>>(loader => loader.Options = options);
        }
    }
}
