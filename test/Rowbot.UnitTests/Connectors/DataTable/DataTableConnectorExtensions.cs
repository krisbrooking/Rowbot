namespace Rowbot.UnitTests.Connectors.DataTable
{
    public static class DataTableConnectorExtensions
    {
        public static IPipelineExtractor<TSource> ExtractDataTable<TSource>(this IPipelineBuilder pipelineBuilder)
        {
            var options = new DataTableConnectorOptions<TSource>();

            return pipelineBuilder.Extract<IDataTableReadConnector<TSource>, TSource, DataTableConnectorOptions<TSource>>(options);
        }

        public static IPipelineLoader<TTarget> LoadDataTable<TSource, TTarget>(this IPipelineTransformer<TSource, TTarget> pipelineTransformer)
        {
            var options = new DataTableConnectorOptions<TTarget>();

            return pipelineTransformer.Load<IDataTableWriteConnector<TTarget>, TTarget, DataTableConnectorOptions<TTarget>>(options);
        }
    }
}
