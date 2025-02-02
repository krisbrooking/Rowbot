using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Rowbot.Entities;
using Rowbot.Connectors.Common.Database;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Transactions;
using Rowbot.Connectors.SqlServer.Extensions;

namespace Rowbot.Connectors.SqlServer
{
    public class SqlServerWriteConnector<TInput> : IWriteConnector<TInput>, ICreateConnector
    {
        private readonly ILogger<SqlServerWriteConnector<TInput>> _logger;
        private readonly IEntity<TInput> _entity;
        private readonly ISqlCommandProvider<TInput, SqlServerCommandProvider> _sqlCommandProvider;
        private Dictionary<string, Action<DataRow, TInput>> _dataRowValueAssigningActions;

        public SqlServerWriteConnector(
            ILogger<SqlServerWriteConnector<TInput>> logger,
            IEntity<TInput> entity,
            ISqlCommandProvider<TInput, SqlServerCommandProvider> sqlCommandProvider)
        {
            _logger = logger;
            _entity = entity;
            _sqlCommandProvider = sqlCommandProvider;

            var dataRowIndexer = typeof(DataRow)
                .GetProperties()
                .First(x =>
                    x.Name == "Item" &&
                    x.GetIndexParameters().Length == 1 &&
                    x.GetIndexParameters()[0].ParameterType == typeof(string));

            _dataRowValueAssigningActions = entity.Descriptor.Value.Fields
                .Where(x => x.DatabaseGeneratedOption != DatabaseGeneratedOption.Identity)
                .Select(x => new { Field = x.Name, Mapper = GetDataRowValueAssigningExpression(x, dataRowIndexer).Compile() })
                .ToDictionary(x => x.Field, x => x.Mapper);
        }

        public SqlServerConnectorOptions Options { get; set; } = new();

        public async Task<IEnumerable<TInput>> FindAsync(
            IEnumerable<TInput> findEntities,
            Action<IFieldSelector<TInput>> compareFieldSelector,
            Action<IFieldSelector<TInput>> resultFieldSelector)
        {
            var result = new List<TInput>();

            using (SqlConnection connection = new SqlConnection(Options.ConnectionString))
            {
                await connection.OpenAsync();

                foreach (SqlCommand findCommand in _sqlCommandProvider.GetFindCommands(findEntities, compareFieldSelector, resultFieldSelector))
                {
                    findCommand.Connection = connection;
                    using (SqlDataReader reader = await findCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var findResult = Activator.CreateInstance<TInput>();

                            for (var ordinal = 0; ordinal < reader.FieldCount; ordinal++)
                            {
                                var mapper = _entity.Accessor.Value.GetValueMapper(_entity.Descriptor.Value.GetField(reader.GetName(ordinal)));
                                mapper(reader.IsDBNull(ordinal) ? null! : reader.GetValue(ordinal), findResult);
                            }

                            result.Add(findResult);
                        }
                    }
                }


                await connection.CloseAsync();
            }

            return result;
        }

        public async Task<int> InsertAsync(IEnumerable<TInput> data)
        {
            if (data.Count() == 0)
            {
                return 0;
            }

            int rowsChanged = 0;

            if (Transaction.Current != null)
            {
                using (SqlConnection connection = new SqlConnection(Options.ConnectionString))
                {
                    await connection.OpenAsync();

                    using (SqlCommand insertCommand = (SqlCommand)_sqlCommandProvider.GetInsertCommand())
                    {
                        insertCommand.Connection = connection;
                        foreach (var item in data)
                        {
                            foreach (FieldDescriptor field in _entity.Descriptor.Value.Fields)
                            {
                                var accessor = _entity.Accessor.Value.GetValueAccessor(field);
                                _sqlCommandProvider.AddOrUpdateParameter(insertCommand, new ExtractParameter(field.Name, field.Property.PropertyType, accessor(item), field.IsNullable));
                            }

                            rowsChanged += await insertCommand.ExecuteNonQueryAsync();
                        }
                    }

                    await connection.CloseAsync();
                }

                return rowsChanged;
            }

            var destinationTableName = string.IsNullOrEmpty(_entity.Descriptor.Value.SchemaName)
                ? $"[{_entity.Descriptor.Value.TableName}]"
                : $"[{_entity.Descriptor.Value.SchemaName}].[{_entity.Descriptor.Value.TableName}]";

            var dataTable = ToDataTable(data);

            using (var bulkCopy = new SqlBulkCopy(Options.ConnectionString))
            {
                bulkCopy.DestinationTableName = destinationTableName;
                bulkCopy.BatchSize = data.Count();

                foreach (DataColumn dtColumn in dataTable.Columns)
                {
                    bulkCopy.ColumnMappings.Add(dtColumn.ColumnName, dtColumn.ColumnName);
                }

                await bulkCopy.WriteToServerAsync(dataTable);

                rowsChanged = bulkCopy.RowsCopied;
            }

            _logger.LogInformation("Rows inserted into {TableName}: {RowsChanged}", destinationTableName, rowsChanged);

            return rowsChanged;
        }

        public async Task<int> UpdateAsync(IEnumerable<RowUpdate<TInput>> data)
        {
            if (data.Count() == 0)
            {
                return 0;
            }

            int rowsChanged = 0;

            using (SqlConnection connection = new SqlConnection(Options.ConnectionString))
            {
                await connection.OpenAsync();

                foreach (SqlCommand updateCommand in _sqlCommandProvider.GetUpdateCommands(data))
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

            var destinationTableName = string.IsNullOrEmpty(_entity.Descriptor.Value.SchemaName)
                ? $"[{_entity.Descriptor.Value.TableName}]"
                : $"[{_entity.Descriptor.Value.SchemaName}].[{_entity.Descriptor.Value.TableName}]";

            _logger.LogInformation("Rows updated into {TableName}: {RowsChanged}", destinationTableName, rowsChanged);

            return rowsChanged;
        }

        public async Task<bool> CreateDataSetAsync()
        {
            int rowsChanged = 0;

            using (SqlConnection connection = new SqlConnection(Options.ConnectionString))
            {
                await connection.OpenAsync();
                
                using (SqlCommand createCommand = (SqlCommand)_sqlCommandProvider.GetCreateDataSetCommand())
                {
                    createCommand.CommandText = CreateDataSetCommandText(createCommand.CommandText);
                    createCommand.Connection = connection;
                    _logger.LogCommand(createCommand);
                    rowsChanged += await createCommand.ExecuteNonQueryAsync();
                }

                await connection.CloseAsync();
            }

            return rowsChanged > 0;
        }

        public async Task<bool> ExecuteCommandAsync(string commandText)
        {
            int rowsChanged = 0;

            using (SqlConnection connection = new SqlConnection(Options.ConnectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand(commandText, connection))
                {
                    command.Connection = connection;
                    _logger.LogCommand(command);
                    rowsChanged += await command.ExecuteNonQueryAsync();
                }

                await connection.CloseAsync();
            }

            return rowsChanged > 0;
        }

        private DataTable ToDataTable(IEnumerable<TInput> data)
        {
            var dataTable = new DataTable();

            foreach (var field in _entity.Descriptor.Value.Fields)
            {
                var column = new DataColumn();
                column.ColumnName = field.Name;
                column.DataType = Nullable.GetUnderlyingType(field.Property.PropertyType) ?? field.Property.PropertyType;
                column.AllowDBNull = field.IsNullable;

                column.AutoIncrement = field.IsKey && field.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity;

                dataTable.Columns.Add(column);
            }

            foreach (var item in data)
            {
                var row = dataTable.NewRow();
                foreach (var action in _dataRowValueAssigningActions)
                {
                    action.Value.Invoke(row, item);
                }
                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        private Expression<Action<DataRow, TInput>> GetDataRowValueAssigningExpression(FieldDescriptor field, PropertyInfo dataRowIndexer)
        {
            var sourceType = typeof(TInput);
            var sourceTypeParameter = Expression.Parameter(sourceType);

            var dataRowType = typeof(DataRow);
            var dataRowTypeParameter = Expression.Parameter(dataRowType);
            var dataRowSetter = Expression.Property(dataRowTypeParameter, dataRowIndexer, Expression.Constant(field.Name));

            if (field.IsNullable)
            {
                var valueEqualsNull = Expression.Equal(Expression.MakeMemberAccess(sourceTypeParameter, field.Property), Expression.Constant(null, field.Property.PropertyType));
                var dbNullValue = Expression.Convert(Expression.Constant(DBNull.Value), typeof(object));
                var isNullCondition = Expression.Condition(valueEqualsNull, dbNullValue, Expression.Convert(Expression.MakeMemberAccess(sourceTypeParameter, field.Property), typeof(object)));

                var body = Expression.Assign(dataRowSetter, isNullCondition);
                var lambda = Expression.Lambda<Action<DataRow, TInput>>(body, dataRowTypeParameter, sourceTypeParameter);
                return lambda;
            }
            else
            {
                var body = Expression.Assign(dataRowSetter, Expression.Convert(Expression.MakeMemberAccess(sourceTypeParameter, field.Property), typeof(object)));
                var lambda = Expression.Lambda<Action<DataRow, TInput>>(body, dataRowTypeParameter, sourceTypeParameter);
                return lambda;
            }
        }

        internal string CreateDataSetCommandText(string commandText)
        {
            var tableName = string.Empty;
            var schemaName = string.Empty;

            commandText = commandText.Replace("AS IDENTITY", "IDENTITY(1,1)");

            var tableAndSchemaName = commandText.Replace("CREATE TABLE IF NOT EXISTS ", "").Split(' ').First().Split('.');
            if (tableAndSchemaName.Length == 1)
            {
                tableName = tableAndSchemaName[0].Trim('[').Trim(']');
            }
            else
            {
                tableName = tableAndSchemaName[1].Trim('[').Trim(']');
                schemaName = tableAndSchemaName[0].Trim('[').Trim(']');
            }

            if (string.IsNullOrEmpty(schemaName))
            {
                commandText = commandText.Replace("CREATE TABLE IF NOT EXISTS ", $"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}') CREATE TABLE ");
            }
            else
            {
                commandText = commandText.Replace("CREATE TABLE IF NOT EXISTS ", $"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{schemaName}' AND TABLE_NAME = '{tableName}') CREATE TABLE ");
            }

            var indexes = commandText.Split("CREATE INDEX IF NOT EXISTS ");
            foreach (var index in indexes.Skip(1))
            {
                var indexParts = index.Split(' ');

                if (string.IsNullOrEmpty(schemaName))
                {
                    commandText = commandText.Replace($"CREATE INDEX IF NOT EXISTS {indexParts[0]}", $"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id] = OBJECT_ID('{tableName}') AND [name] = '{indexParts[0]}') CREATE INDEX {indexParts[0]}");
                }
                else
                {
                    commandText = commandText.Replace($"CREATE INDEX IF NOT EXISTS {indexParts[0]}", $"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id] = OBJECT_ID('{schemaName}.{tableName}') AND [name] = '{indexParts[0]}') CREATE INDEX {indexParts[0]}");
                }
            }

            return commandText;
        }
    }
}
