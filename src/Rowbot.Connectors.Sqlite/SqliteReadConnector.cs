using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Rowbot.Connectors.Sqlite.Extensions;
using Rowbot.Entities;
using Rowbot.Framework.Blocks.Connectors.Database;

namespace Rowbot.Connectors.Sqlite
{
    public class SqliteReadConnector<TSource> : IReadConnector<TSource, SqliteReadConnectorOptions<TSource>>
    {
        private readonly ILogger<SqliteReadConnector<TSource>> _logger;
        protected readonly IEntity<TSource> _entity;
        protected readonly ISqlCommandProvider<TSource, SqliteCommandProvider> _sqlCommandProvider;

        public SqliteReadConnector(
            ILogger<SqliteReadConnector<TSource>> logger,
            IEntity<TSource> entity, 
            ISqlCommandProvider<TSource, SqliteCommandProvider> sqlCommandProvider)
        {
            Options = new();
            _logger = logger;
            _entity = entity;
            _sqlCommandProvider = sqlCommandProvider;
        }

        public SqliteReadConnectorOptions<TSource> Options { get; set; }

        public async Task<IEnumerable<TSource>> QueryAsync(IEnumerable<ExtractParameter> parameters)
        {
            var result = new List<TSource>();

            var rows = 0;
            using (var connection = new SqliteConnection(Options.ConnectionString))
            {
                await connection.OpenAsync();

                foreach (var queryCommand in GetQueryCommands(parameters))
                {
                    queryCommand.Connection = connection;
                    _logger.LogQuery(queryCommand);
                    using (var reader = await queryCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var rowResult = Activator.CreateInstance<TSource>();

                            for (var ordinal = 0; ordinal < reader.FieldCount; ordinal++)
                            {
                                var name = reader.GetName(ordinal);
                                var field = _entity.Descriptor.Value.GetField(name);
                                var mapper = _entity.Accessor.Value.GetValueMapper(field);
                                
                                if (!await reader.IsDBNullAsync(ordinal))
                                {
                                    mapper(SqliteCommandProvider.ConvertType(Nullable.GetUnderlyingType(field.Property.PropertyType) ?? field.Property.PropertyType, reader, ordinal), rowResult);
                                }
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

        internal IEnumerable<SqliteCommand> GetQueryCommands(IEnumerable<ExtractParameter> parameters)
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
                SqliteCommand queryCommand;
                if (Options.HasQuery)
                {
                    queryCommand = new SqliteCommand(Options.Query);
                }
                else
                {
                    queryCommand = (SqliteCommand)_sqlCommandProvider.GetQueryCommand();
                }

                yield return (SqliteCommand)_sqlCommandProvider.AddParameters(queryCommand, parameters);
            }
        }
    }
}
