using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rowbot.Connectors.Sqlite;
using Rowbot.Entities;
using Rowbot.Framework.Blocks.Connectors.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rowbot.IntegrationTests.Sqlite
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

        public static async Task<int> WriteRowsAsync<TTarget>(IEnumerable<TTarget> rows)
        {
            var logger = new LoggerFactory().CreateLogger<SqliteWriteConnector<TTarget>>();
            var entity = new Entity<TTarget>();
            var sqlCommandProvider = new SqlCommandProvider<TTarget, SqliteCommandProvider>(new SqliteCommandProvider(), entity);
            var connector = new SqliteWriteConnector<TTarget>(logger, entity, sqlCommandProvider);
            connector.Options = new SqliteWriteConnectorOptions<TTarget>(ConnectionString);

            await connector.CreateDataSetAsync();

            return await connector.InsertAsync(rows);
        }

        public static async Task<IEnumerable<TTarget>> ReadRowsAsync<TTarget, TProperty>(
            IEnumerable<TTarget> findEntities,
            Action<IFieldSelector<TTarget>> compareFieldSelector,
            Action<IFieldSelector<TTarget>> resultFieldSelector)
        {
            var logger = new LoggerFactory().CreateLogger<SqliteWriteConnector<TTarget>>();
            var entity = new Entity<TTarget>();
            var sqlCommandProvider = new SqlCommandProvider<TTarget, SqliteCommandProvider>(new SqliteCommandProvider(), entity);
            var connector = new SqliteWriteConnector<TTarget>(logger, entity, sqlCommandProvider);
            connector.Options = new SqliteWriteConnectorOptions<TTarget>(ConnectionString);

            return await connector.FindAsync(findEntities, compareFieldSelector, resultFieldSelector);
        }

        public static async Task<IEnumerable<TTarget>> ReadRowsAsync<TTarget>()
        {
            var logger = new LoggerFactory().CreateLogger<SqliteReadConnector<TTarget>>();
            var entity = new Entity<TTarget>();
            var sqlCommandProvider = new SqlCommandProvider<TTarget, SqliteCommandProvider>(new SqliteCommandProvider(), entity);
            var connector = new SqliteReadConnector<TTarget>(logger, entity, sqlCommandProvider);
            connector.Options = new SqliteReadConnectorOptions<TTarget>(ConnectionString);

            return await connector.QueryAsync(Enumerable.Empty<ExtractParameter>());
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
