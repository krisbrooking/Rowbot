using Rowbot.Connectors.Sqlite;

namespace Rowbot
{
    public static class SqliteConnectorExtensions
    {
        /// <summary>
        /// Extracts data from a Sqlite database using a query.
        /// </summary>
        public static IPipelineExtractor<TSource> ExtractSqlite<TSource>(this IPipelineBuilder pipelineBuilder, string connectionString, string query, Action<SqliteReadConnectorOptions<TSource>>? configure = default)
        {
            var options = new SqliteReadConnectorOptions<TSource>();
            options.ConnectionString = connectionString;
            options.Query = query;
            configure?.Invoke(options);

            return pipelineBuilder.Extract<SqliteReadConnector<TSource>, TSource, SqliteReadConnectorOptions<TSource>>(options);
        }

        /// <summary>
        /// Loads data to a Sqlite database table.
        /// </summary>
        public static IPipelineLoader<TTarget> LoadSqlite<TSource, TTarget>(this IPipelineTransformer<TSource, TTarget> pipelineTransformer, string connectionString, Action<SqliteWriteConnectorOptions<TTarget>>? configure = default)
        {
            var options = new SqliteWriteConnectorOptions<TTarget>();
            options.ConnectionString = connectionString;
            configure?.Invoke(options);

            var pipelineLoader = pipelineTransformer.Load<SqliteWriteConnector<TTarget>, TTarget, SqliteWriteConnectorOptions<TTarget>>(options);

            foreach (var pipelineCommand in options.PipelineCommands)
            {
                if (pipelineLoader is ICustomPipelineLoader<TTarget> customLoader)
                {
                    var writeConnector = customLoader.WriteConnectorFactory() as SqliteWriteConnector<TTarget>;
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
