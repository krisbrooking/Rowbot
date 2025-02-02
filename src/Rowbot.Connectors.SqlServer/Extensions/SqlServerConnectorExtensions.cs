using Microsoft.Extensions.Logging;
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
            Action<SqlServerConnectorOptions>? configure = null)
        {
            var options = new SqlServerConnectorOptions();
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
            return connectorStep.WithTask(async (connector, logger) =>
            {
                var entity = new Entity<TInput>();

                var commandText = string.Empty;
                if (string.IsNullOrEmpty(entity.Descriptor.Value.SchemaName))
                {
                    commandText = $"IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{entity.Descriptor.Value.TableName}') TRUNCATE TABLE [{entity.Descriptor.Value.TableName}];";
                    logger.LogInformation("TRUNCATE TABLE [{tableName}]", entity.Descriptor.Value.TableName);
                }
                else
                {
                    commandText = $"IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{entity.Descriptor.Value.SchemaName}' AND TABLE_NAME = '{entity.Descriptor.Value.TableName}') TRUNCATE TABLE [{entity.Descriptor.Value.SchemaName}].[{entity.Descriptor.Value.TableName}];";
                    logger.LogInformation("TRUNCATE TABLE [{schemaName}].[{tableName}]", entity.Descriptor.Value.SchemaName, entity.Descriptor.Value.TableName);
                }

                await connector.ExecuteCommandAsync(commandText);
            }, "Truncate Table", TaskExecutionOrder.PrePipeline, TaskPriority.High);
        }
    }
}
