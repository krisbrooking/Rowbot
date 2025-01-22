using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Rowbot.Connectors.Sqlite.Extensions;
using Rowbot.Entities;
using Rowbot.Connectors.Common.Database;

namespace Rowbot.Connectors.Sqlite
{
    public class SqliteReadConnector<TInput, TOutput>(
        ILogger<SqliteReadConnector<TInput, TOutput>> logger,
        IEntity<TOutput> entity, 
        ISqlCommandProvider<TOutput, SqliteCommandProvider> sqlCommandProvider) : IReadConnector<TInput, TOutput>
    {
        private readonly ILogger<SqliteReadConnector<TInput, TOutput>> _logger = logger;
        private readonly IEntity<TOutput> _entity = entity;
        private readonly ISqlCommandProvider<TOutput, SqliteCommandProvider> _sqlCommandProvider = sqlCommandProvider;

        public SqliteReadConnectorOptions<TOutput> Options { get; set; } = new();

        public async Task<IEnumerable<TOutput>> QueryAsync(ExtractParameter[] parameters)
        {
            var result = new List<TOutput>();

            var rows = 0;
            var errors = 0;
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
                            try
                            {
                                var rowResult = Activator.CreateInstance<TOutput>();

                                for (var ordinal = 0; ordinal < reader.FieldCount; ordinal++)
                                {
                                    var name = reader.GetName(ordinal);
                                    if (!_entity.Descriptor.Value.Fields.Any(x => string.Equals(x.Name, name)))
                                    {
                                        _logger.LogWarning("Row {Row}: Field {Field} returned from query but doesn't exist in entity", rows, name);
                                    }
                                    
                                    var field = _entity.Descriptor.Value.GetField(name);
                                    var mapper = _entity.Accessor.Value.GetValueMapper(field);

                                    if (!field.IsNullable && reader.IsDBNull(ordinal))
                                    {
                                        _logger.LogError("Row {Row}: Field {Field} is not nullable but value returned from query is null", rows, field.Name);
                                        throw new InvalidOperationException($"Field {field.Name} is not nullable but value returned from query is null");
                                    }
                                
                                    if (!await reader.IsDBNullAsync(ordinal))
                                    {
                                        mapper(SqliteCommandProvider.ConvertType(Nullable.GetUnderlyingType(field.Property.PropertyType) ?? field.Property.PropertyType, reader, ordinal), rowResult);
                                    }
                                }
                            
                                result.Add(rowResult);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Row: {Row}: {Message} Parameters {Parameters}",
                                    rows,
                                    ex.Message,
                                    string.Join(";", parameters.Select(p => $"Name: {p.ParameterName}, Value: {p.ParameterValue}")));
                                errors++;
                            }

                            rows++;
                        }
                    }
                    _logger.LogInformation("Query rows returned: {rows}", rows - errors);
                    rows = 0;
                    errors = 0;
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
