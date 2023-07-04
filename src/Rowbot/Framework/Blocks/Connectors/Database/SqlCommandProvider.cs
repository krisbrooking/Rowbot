using Rowbot.Common.Extensions;
using Rowbot.Entities;
using Rowbot.Framework.Blocks.Transformers.Mappers.Transforms;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Text;

namespace Rowbot.Framework.Blocks.Connectors.Database
{
    public interface ISqlCommandProvider<TEntity, TDbCommandProvider> : IDbCommandProvider
        where TDbCommandProvider : IDbCommandProvider
    {
        IDbCommand GetQueryCommand();
        IEnumerable<IDbCommand> GetFindCommands(
            IEnumerable<TEntity> findEntities,
            Action<IFieldSelector<TEntity>> compareFieldSelector,
            Action<IFieldSelector<TEntity>> resultFieldSelector,
            int maxParameters = 100);
        IDbCommand GetInsertCommand();
        IEnumerable<IDbCommand> GetUpdateCommands(
            IEnumerable<Update<TEntity>> updates,
            int maxParameters = 100);
        IDbCommand GetCreateDataSetCommand();
        IDbCommand GetTruncateDataSetCommand();
    }

    public sealed class SqlCommandProvider<TEntity, TDbCommandProvider> : ISqlCommandProvider<TEntity, TDbCommandProvider>
        where TDbCommandProvider : IDbCommandProvider
    {
        private readonly TDbCommandProvider _dbCommandProvider;
        private readonly IEntity<TEntity> _entity;
        private readonly string _table;

        public SqlCommandProvider(TDbCommandProvider dbCommandProvider, IEntity<TEntity> entity)
        {
            _dbCommandProvider = dbCommandProvider;
            _entity = entity;

            _table = _entity.Descriptor.Value.SchemaName is null ? $"[{_entity.Descriptor.Value.TableName}]" : $"[{_entity.Descriptor.Value.SchemaName}].[{_entity.Descriptor.Value.TableName}]";
        }

        /// <summary>
        /// Generates a single SELECT command for all fields in entity
        /// </summary>
        public IDbCommand GetQueryCommand()
        {
            var fields = _entity.Descriptor.Value.Fields.Select(x => $"[{x.Name}]");

            return _dbCommandProvider.CreateCommand($"SELECT {string.Join(',', fields)} FROM {_table}");
        }

        /// <summary>
        /// Generates one or more SELECT commands that each query one or more entities. The number of
        /// commands generated depends on the maximum number of parameters supported.
        /// </summary>
        public IEnumerable<IDbCommand> GetFindCommands(
            IEnumerable<TEntity> findEntities,
            Action<IFieldSelector<TEntity>> compareFieldSelector,
            Action<IFieldSelector<TEntity>> resultFieldSelector,
            int maxParameters = 100)
        {
            FieldSelector<TEntity> compareFields = new();
            compareFieldSelector.Invoke(compareFields);

            FieldSelector<TEntity> resultFields = new();
            resultFieldSelector.Invoke(resultFields);

            var extractParameters = findEntities
                .Select((entity, entityIndex) =>
                {
                    return compareFields.Selected.Select((field, fieldIndex) =>
                    {
                        var accessor = _entity.Accessor.Value.GetValueAccessor(field);
                        return (Field: field, Parameter: new ExtractParameter($"@p{entityIndex}{field.Name}", field.Property.PropertyType, accessor(entity)));
                    });
                })
                .Chunk(maxParameters / compareFields.Selected.Count());

            foreach (var commandParameters in extractParameters)
            {
                var whereConditions = string.Join(" OR ", commandParameters.Select(parameter => $"({string.Join(" AND ", parameter.Select(field => $"[{field.Field.Name}] = {field.Parameter.ParameterName}"))})"));

                var selectedFindFields = compareFields.Selected.Select(x => $"[{x.Name}]");

                var selectedResultFields = resultFields.Selected
                    .Where(x => !compareFields.Selected.Select(x => x.Property.Name).Contains(x.Property.Name))
                    .Select(x => $"[{x.Name}]");

                var query = $"SELECT {string.Join(',', selectedFindFields)},{string.Join(',', selectedResultFields)} FROM {_table} WHERE {whereConditions}";
                var command = _dbCommandProvider.CreateCommand(query);
                _dbCommandProvider.AddParameters(command, commandParameters.SelectMany(x => x).Select(x => x.Parameter));

                yield return command;
            }
        }

        /// <summary>
        /// Generates a single INSERT command. Includes all fields where values are not database generated.
        /// </summary>
        public IDbCommand GetInsertCommand()
        {
            var fields = _entity.Descriptor.Value.Fields
                .Where(x => x.DatabaseGeneratedOption == DatabaseGeneratedOption.None)
                .OrderBy(x => x.Name);

            var commandText = new StringBuilder();
            commandText.Append($"INSERT INTO {_table} ({string.Join(',', fields.Select(x => $"[{x.Name}]"))}) ");
            commandText.Append($"VALUES ({string.Join(',', fields.Select(x => $"@{x.Name}"))})");

            var command = _dbCommandProvider.CreateCommand(commandText.ToString());

            foreach (FieldDescriptor field in fields)
            {
                _dbCommandProvider.AddOrUpdateParameter(command, new ExtractParameter(field.Name, field.Property.PropertyType, default!));
            }

            return command;
        }

        /// <summary>
        /// Generates one or more UPDATE commands. Combines multiple updates into a single command using CASE statements.
        /// </summary>
        public IEnumerable<IDbCommand> GetUpdateCommands(
            IEnumerable<Update<TEntity>> updates,
            int maxParameters = 100)
        {
            if (_entity.Descriptor.Value.KeyFields.Count() == 0)
            {
                throw new InvalidOperationException($"{typeof(TEntity)} must specify a unique key with the {nameof(KeyAttribute)} attribute");
            }

            Func<TEntity, byte[]> hashCodeGenerator = new HashCodeTransform<TEntity>().All().Build();
            Dictionary<string, int> updateParameterIds = new();

            foreach (var commandUpdates in updates.ChunkBySum(maxParameters, x => x.ChangedFields.Count() + _entity.Descriptor.Value.KeyFields.Count() * 2))
            {
                updateParameterIds.Clear();

                var commandText = new StringBuilder();
                commandText.AppendLine($"UPDATE {_table} SET ");

                var caseStatements = BuildCaseStatements(commandUpdates, updateParameterIds, hashCodeGenerator);
                commandText.Append(string.Join(", ", caseStatements));

                var whereConditions = BuildWhereConditions(commandUpdates, updateParameterIds, hashCodeGenerator);
                commandText.Append($"WHERE {string.Join(" OR ", whereConditions)}");

                var command = _dbCommandProvider.CreateCommand(commandText.ToString());
                command = AddParametersToCommand(command, commandUpdates, updateParameterIds, hashCodeGenerator);

                yield return command;
            }

            IEnumerable<string> BuildCaseStatements(IEnumerable<Update<TEntity>> updates, Dictionary<string, int> updateParameterIds, Func<TEntity, byte[]> hashCodeGenerator)
            {
                var updateParameterId = 0;

                Dictionary<FieldDescriptor, IEnumerable<string>> updatesByChangedField = GroupByChangedField(updates, hashCodeGenerator);

                var caseStatements = updatesByChangedField
                    .Select((changedField, changedFieldIndex) =>
                    {
                        var updatesWithChangedField = changedField.Value
                            .Select((updateHashCode, updateHashCodeIndex) =>
                            {
                                if (!updateParameterIds.ContainsKey(updateHashCode))
                                {
                                    updateParameterIds.Add(updateHashCode, updateParameterId++);
                                }

                                var whenCondition = string.Join(" AND ", _entity.Descriptor.Value.KeyFields.Select(field => $"[{field.Name}] = @p{updateParameterIds[updateHashCode]}{field.Name}"));
                                var thenStatement = $"@p{updateParameterIds[updateHashCode]}{changedField.Key.Name}";

                                return $"WHEN {whenCondition} THEN {thenStatement}";
                            });

                        return $"[{changedField.Key.Name}] = CASE {string.Join(' ', updatesWithChangedField)} ELSE [{changedField.Key.Name}] END\r\n";
                    });

                return caseStatements;
            }

            Dictionary<FieldDescriptor, IEnumerable<string>> GroupByChangedField(
                IEnumerable<Update<TEntity>> updates,
                Func<TEntity, byte[]> hashCodeGenerator) =>
                    updates
                    .SelectMany(x => x.ChangedFields
                        .Select(field =>
                        {
                            return new { ChangedField = field, HashCode = Convert.ToBase64String(hashCodeGenerator(x.Row)) };
                        })
                    )
                    .GroupBy(x => x.ChangedField)
                    .Select(x => new { ChangedField = x.Key, Items = x.ToList() })
                    .ToDictionary(x => x.ChangedField, x => x.Items.Select(s => s.HashCode));

            IEnumerable<string> BuildWhereConditions(
                IEnumerable<Update<TEntity>> updates,
                Dictionary<string, int> updateParameterIds,
                Func<TEntity, byte[]> hashCodeGenerator) =>
                    updates
                    .Select(update =>
                    {
                        var updateHashCode = Convert.ToBase64String(hashCodeGenerator(update.Row));

                        var conditions = _entity.Descriptor.Value.KeyFields
                            .Select(field => $"[{field.Name}] = @p{updateParameterIds[updateHashCode]}{field.Name}");

                        return $"({string.Join(" AND ", conditions)})";
                    });

            IDbCommand AddParametersToCommand(
                IDbCommand command,
                IEnumerable<Update<TEntity>> data,
                Dictionary<string, int> updateParameterIds,
                Func<TEntity, byte[]> hashCodeGenerator)
            {
                foreach (var item in data)
                {
                    var itemHashCode = Convert.ToBase64String(hashCodeGenerator(item.Row));

                    foreach (var field in _entity.Descriptor.Value.KeyFields)
                    {
                        var accessor = _entity.Accessor.Value.GetValueAccessor(field);
                        _dbCommandProvider.AddOrUpdateParameter(command, new ExtractParameter($@"@p{updateParameterIds[itemHashCode]}{field.Name}", field.Property.PropertyType, accessor(item.Row)));
                    }

                    foreach (var changedField in item.ChangedFields)
                    {
                        var accessor = _entity.Accessor.Value.GetValueAccessor(changedField);
                        _dbCommandProvider.AddOrUpdateParameter(command, new ExtractParameter($@"@p{updateParameterIds[itemHashCode]}{changedField.Name}", changedField.Property.PropertyType, accessor(item.Row)));
                    }
                }

                return command;
            }
        }

        /// <summary>
        /// Generates a single CREATE TABLE command. Due to differences in syntax between DBMS's, some command
        /// statements are returned as tokens that need to be replaced by the calling connector.
        /// </summary>
        public IDbCommand GetCreateDataSetCommand()
        {
            var commandText = new StringBuilder();
            commandText.AppendLine($"CREATE TABLE IF NOT EXISTS {_table} (");

            var tableColumnDefinition = new List<string>();
            var tableColumnDefinitionParts = new List<string>();

            foreach (var field in _entity.Descriptor.Value.Fields
                .Select(x => new
                {
                    ColumnOrder = x.ColumnOrder is null || x.ColumnOrder < 0 ? int.MaxValue : x.ColumnOrder,
                    Field = x
                })
                .OrderBy(x => x.ColumnOrder)
                .ThenByDescending(x => x.Field.IsKey)
                .ThenBy(x => x.Field.Name)
                .Select(x => x.Field))
            {
                tableColumnDefinitionParts.Add($"[{field.Name}]");

                var databaseType = _dbCommandProvider.GetDatabaseType(field);
                if (field.Property.PropertyType == typeof(decimal) ||
                    Nullable.GetUnderlyingType(field.Property.PropertyType) is not null && Nullable.GetUnderlyingType(field.Property.PropertyType) == typeof(decimal))
                {
                    databaseType = $"DECIMAL({field.Precision}, {field.Scale})";
                }
                tableColumnDefinitionParts.Add(databaseType);

                if (field.IsRequired)
                {
                    tableColumnDefinitionParts.Add("NOT NULL");
                }
                else
                {
                    if (field.IsNullable)
                    {
                        tableColumnDefinitionParts.Add("NULL");
                    }
                    else
                    {
                        tableColumnDefinitionParts.Add("NOT NULL");
                    }
                }

                if (field.IsUnique)
                {
                    tableColumnDefinitionParts.Add("UNIQUE");
                }
                if (field.IsKey && !_entity.Descriptor.Value.HasCompositeKey)
                {
                    tableColumnDefinitionParts.Add("PRIMARY KEY");
                }
                if (field.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity)
                {
                    tableColumnDefinitionParts.Add("AS IDENTITY");
                }

                tableColumnDefinition.Add(string.Join(' ', tableColumnDefinitionParts));

                tableColumnDefinitionParts.Clear();
            }

            foreach (var foreignKeyField in _entity.Descriptor.Value.ForeignKeyFields)
            {
                tableColumnDefinition.Add($"FOREIGN KEY ([{foreignKeyField.Name}]) REFERENCES [{foreignKeyField.ForeignKeyPrincipalName?.Trim('[').Trim(']')}]([{foreignKeyField.ForeignKeyPrincipalKey?.Trim('[').Trim(']')}])");
            }

            if (_entity.Descriptor.Value.HasCompositeKey)
            {
                tableColumnDefinition.Add($"PRIMARY KEY({string.Join(',', _entity.Descriptor.Value.KeyFields.Select(x => $"[{x.Name}]"))})");
            }

            commandText.AppendLine(string.Join(",\r\n", tableColumnDefinition));

            commandText.Append(");");
            if (typeof(TEntity).IsAssignableTo(typeof(Row)))
            {
                commandText.Append($"CREATE INDEX IF NOT EXISTS idx_{_entity.Descriptor.Value.TableName}_KeyHash ON {_table} (KeyHash);");
            }
            if (typeof(TEntity).IsAssignableTo(typeof(Dimension)))
            {
                commandText.Append($"CREATE INDEX IF NOT EXISTS idx_{_entity.Descriptor.Value.TableName}_KeyHash_IsActive ON {_table} (KeyHash, IsActive);");
            }

            return _dbCommandProvider.CreateCommand(commandText.ToString());
        }

        /// <summary>
        /// Generates a single TRUNCATE TABLE command.
        /// </summary>
        public IDbCommand GetTruncateDataSetCommand() => _dbCommandProvider.CreateCommand($"TRUNCATE TABLE {_table}");

        public IDbCommand CreateCommand(string commandText) => _dbCommandProvider.CreateCommand(commandText);

        public IDbCommand AddOrUpdateParameter(IDbCommand command, ExtractParameter extractParameter) => _dbCommandProvider.AddOrUpdateParameter(command, extractParameter);

        public IDbCommand AddParameters(IDbCommand command, IEnumerable<ExtractParameter> extractParameters) => _dbCommandProvider.AddParameters(command, extractParameters);

        public string GetDatabaseType(FieldDescriptor field) => _dbCommandProvider.GetDatabaseType(field);
    }
}
