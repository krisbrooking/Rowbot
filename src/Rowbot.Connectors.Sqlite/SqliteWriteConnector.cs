using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Rowbot.Connectors.Sqlite.Extensions;
using Rowbot.Entities;
using Rowbot.Framework.Blocks.Connectors.Database;

namespace Rowbot.Connectors.Sqlite
{
    public class SqliteWriteConnector<TTarget> : IWriteConnector<TTarget, SqliteWriteConnectorOptions<TTarget>>, ISchemaConnector
    {
        private readonly ILogger<SqliteWriteConnector<TTarget>> _logger;
        protected readonly IEntity<TTarget> _entity;
        protected readonly ISqlCommandProvider<TTarget, SqliteCommandProvider> _sqlCommandProvider;

        public SqliteWriteConnector(
            ILogger<SqliteWriteConnector<TTarget>> logger,
            IEntity<TTarget> entity,
            ISqlCommandProvider<TTarget, SqliteCommandProvider> sqlCommandProvider)
        {
            Options = new();
            _logger = logger;
            _entity = entity;
            _sqlCommandProvider = sqlCommandProvider;
        }

        public SqliteWriteConnectorOptions<TTarget> Options { get; set; }

        public async Task<IEnumerable<TTarget>> FindAsync(
            IEnumerable<TTarget> findEntities,
            Action<IFieldSelector<TTarget>> compareFieldSelector,
            Action<IFieldSelector<TTarget>> resultFieldSelector)
        {
            var result = new List<TTarget>();

            using (var connection = new SqliteConnection(Options.ConnectionString))
            {
                await connection.OpenAsync();

                foreach (SqliteCommand findCommand in _sqlCommandProvider.GetFindCommands(findEntities, compareFieldSelector, resultFieldSelector))
                {
                    findCommand.Connection = connection;
                    using (SqliteDataReader reader = await findCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var findResult = Activator.CreateInstance<TTarget>();

                            for (var ordinal = 0; ordinal < reader.FieldCount; ordinal++)
                            {
                                var name = reader.GetName(ordinal);
                                var field = _entity.Descriptor.Value.GetField(name);
                                var mapper = _entity.Accessor.Value.GetValueMapper(field);

                                if (!await reader.IsDBNullAsync(ordinal))
                                {
                                    mapper(SqliteCommandProvider.ConvertType(Nullable.GetUnderlyingType(field.Property.PropertyType) ?? field.Property.PropertyType, reader, ordinal), findResult);
                                }
                            }

                            result.Add(findResult);
                        }
                    }
                }

                await connection.CloseAsync();
            }

            return result;
        }

        public async Task<int> InsertAsync(IEnumerable<TTarget> data)
        {
            int rowsChanged = 0;

            using (var connection = new SqliteConnection(Options.ConnectionString))
            {
                await connection.OpenAsync();

                // Create a single command and reuse it multiple times by modifying the parameters
                using (var command = (SqliteCommand)_sqlCommandProvider.GetInsertCommand())
                {
                    command.Connection = connection;
                    foreach (var item in data)
                    {
                        foreach (FieldDescriptor field in _entity.Descriptor.Value.Fields)
                        {
                            var accessor = _entity.Accessor.Value.GetValueAccessor(field);
                            _sqlCommandProvider.AddOrUpdateParameter(command, new ExtractParameter(field.Name, field.Property.PropertyType, accessor(item)));
                        }

                        rowsChanged += await command.ExecuteNonQueryAsync();
                    }
                }

                await connection.CloseAsync();
            }

            return rowsChanged;
        }

        public async Task<int> UpdateAsync(IEnumerable<Update<TTarget>> data)
        {
            int rowsChanged = 0;

            using (var connection = new SqliteConnection(Options.ConnectionString))
            {
                await connection.OpenAsync();

                foreach (SqliteCommand updateCommand in _sqlCommandProvider.GetUpdateCommands(data))
                {
                    _logger.LogCommand(updateCommand);
                    updateCommand.Connection = connection;
                    using (updateCommand)
                    {
                        rowsChanged += await updateCommand.ExecuteNonQueryAsync();
                    }
                }

                await connection.CloseAsync();
            }

            return rowsChanged;
        }

        public async Task<bool> CreateDataSetAsync()
        {
            int rowsChanged = 0;

            using (SqliteConnection connection = new SqliteConnection(Options.ConnectionString))
            {
                await connection.OpenAsync();

                if (Options.TruncateTable)
                {
                    using (SqliteCommand truncateCommand = (SqliteCommand)_sqlCommandProvider.GetTruncateDataSetCommand())
                    {
                        _logger.LogCommand(truncateCommand);
                        truncateCommand.Connection = connection;
                        rowsChanged += await truncateCommand.ExecuteNonQueryAsync();
                    }
                }

                using (SqliteCommand createCommand = (SqliteCommand)_sqlCommandProvider.GetCreateDataSetCommand())
                {
                    createCommand.CommandText = CreateDataSetCommandText(createCommand.CommandText);
                    _logger.LogCommand(createCommand);
                    createCommand.Connection = connection;
                    rowsChanged += await createCommand.ExecuteNonQueryAsync();
                }

                await connection.CloseAsync();
            }

            return rowsChanged > 0;
        }

        internal string CreateDataSetCommandText(string commandText)
        {
            commandText = commandText.Replace("AS IDENTITY", "");

            return commandText;
        }

        internal async Task<bool> ExecuteCommandAsync(string commandText)
        {
            int rowsChanged = 0;

            using (SqliteConnection connection = new SqliteConnection(Options.ConnectionString))
            {
                await connection.OpenAsync();

                using (SqliteCommand command = new SqliteCommand(commandText, connection))
                {
                    command.Connection = connection;
                    _logger.LogCommand(command);
                    rowsChanged += await command.ExecuteNonQueryAsync();
                }

                await connection.CloseAsync();
            }

            return rowsChanged > 0;
        }
    }
}
