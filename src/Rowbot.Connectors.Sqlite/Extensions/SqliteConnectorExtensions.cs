using Rowbot.Entities;
using Rowbot.Pipelines.Tasks;

namespace Rowbot.Connectors.Sqlite
{
    public static class SqliteConnectorExtensions
    {
        /// <summary>
        /// Extracts data from a Sqlite database using a query.
        /// </summary>
        public static IExtractBuilderConnectorStep<TInput, TOutput> FromSqlite<TInput, TOutput>(
            this IExtractBuilder<TInput, TOutput> builder, 
            string connectionString, 
            string query, 
            Action<SqliteReadConnectorOptions<TOutput>>? configure = null)
        {
            var options = new SqliteReadConnectorOptions<TOutput>(connectionString);
            options.Query = query;
            configure?.Invoke(options);

            return builder.WithConnector<SqliteReadConnector<TInput, TOutput>>(extractor => extractor.Options = options);
        }

        /// <summary>
        /// Loads data to a Sqlite database table.
        /// </summary>
        public static ILoadBuilderConnectorStep<TInput, SqliteWriteConnector<TInput>> ToSqlite<TInput>(
            this ILoadBuilder<TInput> builder, 
            string connectionString, 
            Action<SqliteConnectorOptions>? configure = null)
        {
            var options = new SqliteConnectorOptions(connectionString);
            configure?.Invoke(options);

            return builder.WithConnector<SqliteWriteConnector<TInput>>(loader => loader.Options = options);
        }

        /// <summary>
        /// Truncates a Sqlite database table
        /// </summary>
        public static ILoadBuilderConnectorStep<TInput, SqliteWriteConnector<TInput>> TruncateTable<TInput>(
            this ILoadBuilderConnectorStep<TInput, SqliteWriteConnector<TInput>> connectorStep)
        {
            var entity = new Entity<TInput>();
            
            return connectorStep.WithTask(async connector =>
            {
                await connector.ExecuteCommandAsync($"TRUNCATE TABLE {entity.Descriptor.Value.TableName}");
            }, "Truncate Table", TaskExecutionOrder.PrePipeline, TaskPriority.High);
        }
    }
}
