using Rowbot.Entities;
using Rowbot.Pipelines.Tasks;

namespace Rowbot.Connectors.SqlServer
{
    public static class SqlServerConnectorExtensions
    {
        /// <summary>
        /// Extracts data from a MS SQL Server database using a query.
        /// </summary>
        public static IExtractBuilderConnectorStep<TInput, TOutput> FromSqlServer<TInput, TOutput>(
            this IExtractBuilder<TInput, TOutput> builder, 
            string connectionString, 
            string query, 
            Action<SqlServerReadConnectorOptions<TOutput>>? configure = null)
        {
            var options = new SqlServerReadConnectorOptions<TOutput>();
            options.ConnectionString = connectionString;
            options.Query = query;
            configure?.Invoke(options);

            return builder.WithConnector<SqlServerReadConnector<TInput, TOutput>>(connector => connector.Options = options);
        }

        /// <summary>
        /// Loads data to a MS SQL Server database table.
        /// </summary>
        public static ILoadBuilderConnectorStep<TInput, SqlServerWriteConnector<TInput>> ToSqlServer<TInput>(
            this ILoadBuilder<TInput> builder, 
            string connectionString, 
            Action<SqlServerWriteConnectorOptions<TInput>>? configure = null)
        {
            var options = new SqlServerWriteConnectorOptions<TInput>();
            options.ConnectionString = connectionString;
            configure?.Invoke(options);

            return builder.WithConnector<SqlServerWriteConnector<TInput>>(connector => connector.Options = options);
        }

        /// <summary>
        /// Truncates a MS SQL Server database table
        /// </summary>
        public static ILoadBuilderConnectorStep<TInput, SqlServerWriteConnector<TInput>> TruncateTable<TInput>(
            this ILoadBuilderConnectorStep<TInput, SqlServerWriteConnector<TInput>> connectorStep)
        {
            var entity = new Entity<TInput>();
            
            return connectorStep.WithTask(async connector =>
            {
                await connector.ExecuteCommandAsync($"TRUNCATE TABLE {entity.Descriptor.Value.TableName}");
            }, "Truncate Table", TaskExecutionOrder.PrePipeline, TaskPriority.High);
        }
    }
}
