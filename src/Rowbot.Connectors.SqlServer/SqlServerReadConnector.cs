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
            var errors = 0;
            using (var connection = new SqlConnection(Options.ConnectionString))
            {
                connection.RetryLogicProvider = Options.SqlRetryLogicBaseProvider;
                await connection.OpenAsync();

                foreach (SqlCommand queryCommand in GetQueryCommands(parameters))
                {
                    _logger.LogQuery(queryCommand);
                    queryCommand.Connection = connection;
                    using (SqlDataReader reader = await queryCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            try
                            {
                                var rowResult = Activator.CreateInstance<TOutput>();

                                for (var ordinal = 0; ordinal < reader.FieldCount; ordinal++)
                                {
                                    var name = reader.GetName(ordinal);
                                    if (!_entity.Descriptor.Value.Fields.Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        _logger.LogWarning("Row {Row}: Field {Field} returned from query but doesn't exist in entity {Entity}", rows, name, typeof(TOutput).Name);
                                    }

                                    var field = _entity.Descriptor.Value.GetField(name);
                                    var mapper = _entity.Accessor.Value.GetValueMapper(field);

                                    if (!field.IsNullable && reader.IsDBNull(ordinal))
                                    {
                                        _logger.LogError("Row {Row}: Field {Field} is not nullable but value returned from query is null", rows, field.Name);
                                        throw new InvalidOperationException($"Field {field.Name} is not nullable but value returned from query is null");
                                    }

                                    if (!reader.IsDBNull(ordinal))
                                    {
                                        mapper(reader.GetValue(ordinal), rowResult);
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
