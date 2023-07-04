using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Rowbot.Entities;
using Rowbot.Framework.Blocks.Connectors.Database;

namespace Rowbot.Connectors.SqlServer
{
    public class SqlServerReadConnector<TSource> : IReadConnector<TSource, SqlServerReadConnectorOptions<TSource>>
    {
        private readonly ILogger<SqlServerReadConnector<TSource>> _logger;
        private readonly IEntity<TSource> _entity;
        private readonly ISqlCommandProvider<TSource, SqlServerCommandProvider> _sqlCommandProvider;

        public SqlServerReadConnector(
            ILogger<SqlServerReadConnector<TSource>> logger,
            IEntity<TSource> entity,
            ISqlCommandProvider<TSource, SqlServerCommandProvider> sqlCommandProvider)
        {
            Options = new();
            _logger = logger;
            _entity = entity;
            _sqlCommandProvider = sqlCommandProvider;
        }

        public SqlServerReadConnectorOptions<TSource> Options { get; set; }

        public async Task<IEnumerable<TSource>> QueryAsync(IEnumerable<ExtractParameter> parameters)
        {
            var result = new List<TSource>();

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
                            var rowResult = Activator.CreateInstance<TSource>();

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
