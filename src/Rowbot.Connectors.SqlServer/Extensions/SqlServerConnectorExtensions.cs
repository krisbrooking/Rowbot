using Rowbot.Connectors.SqlServer;

namespace Rowbot
{
    public static class SqlServerConnectorExtensions
    {
        /// <summary>
        /// Extracts data from a MS SQL Server database using a query.
        /// </summary>
        public static IPipelineExtractor<TSource> ExtractSqlServer<TSource>(this IPipelineBuilder pipelineBuilder, string connectionString, string query, Action<SqlServerReadConnectorOptions<TSource>>? configure = default)
        {
            var options = new SqlServerReadConnectorOptions<TSource>();
            options.ConnectionString = connectionString;
            options.Query = query;
            configure?.Invoke(options);

            return pipelineBuilder.Extract<SqlServerReadConnector<TSource>, TSource, SqlServerReadConnectorOptions<TSource>>(options);
        }

        /// <summary>
        /// Loads data to a MS SQL Server table.
        /// </summary>
        public static IPipelineLoader<TTarget> LoadSqlServer<TSource, TTarget>(this IPipelineTransformer<TSource, TTarget> pipelineTransformer, string connectionString, Action<SqlServerWriteConnectorOptions<TTarget>>? configure = default)
        {
            var options = new SqlServerWriteConnectorOptions<TTarget>();
            options.ConnectionString = connectionString;
            configure?.Invoke(options);

            var pipelineLoader = pipelineTransformer.Load<SqlServerWriteConnector<TTarget>, TTarget, SqlServerWriteConnectorOptions<TTarget>>(options);

            foreach (var pipelineCommand in options.PipelineCommands)
            {
                if (pipelineLoader is ICustomPipelineLoader<TTarget> customLoader)
                {
                    var writeConnector = customLoader.WriteConnectorFactory() as SqlServerWriteConnector<TTarget>;
                    if (writeConnector is { })
                    {
                        customLoader.AddPipelineBlock(async () =>
                        {
                            await writeConnector.ExecuteCommandAsync(pipelineCommand.Command);
                        },
                        pipelineCommand.Priority);
                    }
                }
            }

            return pipelineLoader;
        }
    }
}
