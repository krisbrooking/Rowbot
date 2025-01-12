using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rowbot.Connectors.Sqlite;
using Rowbot.Entities;
using Rowbot.Connectors.Common.Database;
using System.Text;

namespace Rowbot.IntegrationTests.Setup
{
    public class SqliteTest
    {
        public static string ConnectionString = "Data Source=.\\integrationtests.db";

        public static IPipelineRunner BuildRunner(params Type[] pipelineTypes)
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddRowbot(pipelineTypes);
                    services.AddSqliteConnector();
                })
                .Build();

            return host.Services.GetRequiredService<IPipelineRunner>();
        }

        public static async Task<int> WriteRowsAsync<TInput>(IEnumerable<TInput> rows)
        {
            var logger = new LoggerFactory().CreateLogger<SqliteWriteConnector<TInput>>();
            var entity = new Entity<TInput>();
            var sqlCommandProvider = new SqlCommandProvider<TInput, SqliteCommandProvider>(new SqliteCommandProvider(), entity);
            var connector = new SqliteWriteConnector<TInput>(logger, entity, sqlCommandProvider);
            connector.Options = new SqliteConnectorOptions(ConnectionString);

            await connector.CreateDataSetAsync();

            return await connector.InsertAsync(rows);
        }

        public static async Task<IEnumerable<TOutput>> ReadRowsAsync<TOutput, TProperty>(
            IEnumerable<TOutput> findEntities,
            Action<IFieldSelector<TOutput>> compareFieldSelector,
            Action<IFieldSelector<TOutput>> resultFieldSelector)
        {
            var logger = new LoggerFactory().CreateLogger<SqliteWriteConnector<TOutput>>();
            var entity = new Entity<TOutput>();
            var sqlCommandProvider = new SqlCommandProvider<TOutput, SqliteCommandProvider>(new SqliteCommandProvider(), entity);
            var connector = new SqliteWriteConnector<TOutput>(logger, entity, sqlCommandProvider);
            connector.Options = new SqliteConnectorOptions(ConnectionString);

            return await connector.FindAsync(findEntities, compareFieldSelector, resultFieldSelector);
        }

        public static async Task<IEnumerable<TOutput>> ReadRowsAsync<TOutput>()
        {
            var logger = new LoggerFactory().CreateLogger<SqliteReadConnector<TOutput, TOutput>>();
            var entity = new Entity<TOutput>();
            var sqlCommandProvider = new SqlCommandProvider<TOutput, SqliteCommandProvider>(new SqliteCommandProvider(), entity);
            var connector = new SqliteReadConnector<TOutput, TOutput>(logger, entity, sqlCommandProvider);
            connector.Options = new SqliteReadConnectorOptions<TOutput>(ConnectionString);

            return await connector.QueryAsync([]);
        }

        public static int Reset()
        {
            int rowsChanged = 0;

            using (SqliteConnection connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                var commandText = new StringBuilder();
                commandText.Append("DROP TABLE IF EXISTS [SourceOrderLine];");
                commandText.Append("DROP TABLE IF EXISTS [OrderLine];");
                commandText.Append("DROP TABLE IF EXISTS [SourceCustomer];");
                commandText.Append("DROP TABLE IF EXISTS [SecondSourceCustomer];");
                commandText.Append("DROP TABLE IF EXISTS [Customer];");
                commandText.Append("DROP TABLE IF EXISTS [SourceProduct];");
                commandText.Append("DROP TABLE IF EXISTS [Product];");
                commandText.Append("DROP TABLE IF EXISTS [SourceOrder];");
                commandText.Append("DROP TABLE IF EXISTS [Order];");

                using (SqliteCommand command = new SqliteCommand(commandText.ToString()))
                {
                    command.Connection = connection;
                    rowsChanged += command.ExecuteNonQuery();
                }

                connection.Open();
            }

            return rowsChanged;
        }

        public static int Reset(string tableName)
        {
            int rowsChanged = 0;

            using (SqliteConnection connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                using (SqliteCommand command = new SqliteCommand($"DROP TABLE IF EXISTS [{tableName}];"))
                {
                    command.Parameters.AddWithValue("$TableName", tableName);
                    command.Connection = connection;
                    rowsChanged += command.ExecuteNonQuery();
                }

                connection.Open();
            }

            return rowsChanged;
        }
    }
}
