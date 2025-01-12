using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Rowbot.Entities;
using Rowbot.Connectors.Common.Database;
using Rowbot.Connectors.SqlServer.Extensions;

namespace Rowbot.Connectors.SqlServer
{
    public class SqlServerReadConnector<TInput, TOutput>(
        ILogger<SqlServerReadConnector<TInput, TOutput>> logger,
        IEntity<TOutput> entity,
        ISqlCommandProvider<TOutput, SqlServerCommandProvider> sqlCommandProvider) : IReadConnector<TInput, TOutput>
    {
        private readonly ILogger<SqlServerReadConnector<TInput, TOutput>> _logger = logger;
        private readonly IEntity<TOutput> _entity = entity;
        private readonly ISqlCommandProvider<TOutput, SqlServerCommandProvider> _sqlCommandProvider = sqlCommandProvider;

        public SqlServerReadConnectorOptions<TOutput> Options { get; set; } = new();

        public async Task<IEnumerable<TOutput>> QueryAsync(ExtractParameter[] parameters)
        {
            var result = new List<TOutput>();

            var rows = 0;
            using (var connection = new SqlConnection(Options.ConnectionString))
            {
                await connection.OpenAsync();

                foreach (SqlCommand queryCommand in GetQueryCommands(parameters))
                {
                    _logger.LogQuery(queryCommand);
                    queryCommand.Connection = connection;
                    using (SqlDataReader reader = await queryCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var rowResult = Activator.CreateInstance<TOutput>();

                            for (var ordinal = 0; ordinal < reader.FieldCount; ordinal++)
                            {
                                var mapper = _entity.Accessor.Value.GetValueMapper(_entity.Descriptor.Value.GetField(reader.GetName(ordinal)));
                                mapper(reader.IsDBNull(ordinal) ? null! : reader.GetValue(ordinal), rowResult);
                            }

                            rows++;
                            result.Add(rowResult);
                        }
                    }
                    _logger.LogInformation("Query rows returned: {rows}", rows);
                    rows = 0;
                }

                await connection.CloseAsync();
            }

            return result;
        }

        internal IEnumerable<SqlCommand> GetQueryCommands(IEnumerable<ExtractParameter> parameters)
        {
            if (Options.HasCommands)
            {
                foreach (var command in Options.Commands!)
                {
                    yield return command;
                }
            }
            else
            {
                SqlCommand queryCommand;
                if (Options.HasQuery)
                {
                    queryCommand = new SqlCommand(Options.Query);
                }
                else
                {
                    queryCommand = (SqlCommand)_sqlCommandProvider.GetQueryCommand();
                }

                yield return (SqlCommand)_sqlCommandProvider.AddParameters(queryCommand, parameters);
            }
        }
    }
}
