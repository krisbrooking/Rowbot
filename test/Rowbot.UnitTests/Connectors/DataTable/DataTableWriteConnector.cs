using Rowbot.Entities;
using Rowbot.Connectors.Common.Find;
using Rowbot.Connectors.Common.Synchronisation;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace Rowbot.UnitTests.Connectors.DataTable
{
    public sealed class DataTableWriteConnector<TInput> : IWriteConnector<TInput>
    {
        private readonly IEntity<TInput> _entity;
        private readonly ISharedLockManager _sharedLockManager;
        private readonly IFinderProvider _finderProvider;
        private readonly Dictionary<string, Action<DataRow, TInput>> _dataRowValueAssigningActions;
        private System.Data.DataTable? _dataTable;

        public DataTableWriteConnector(IEntity<TInput> entity, ISharedLockManager sharedLockManager, IFinderProvider finderProvider)
        {
            _entity = entity;
            _sharedLockManager = sharedLockManager;
            _finderProvider = finderProvider;

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

        public DataTableConnectorOptions<TInput> Options { get; set; } = new();

        public Task<IEnumerable<TInput>> FindAsync(
            IEnumerable<TInput> findEntities,
            Action<IFieldSelector<TInput>> compareFieldSelector,
            Action<IFieldSelector<TInput>> resultFieldSelector)
        {
            var results = new List<TInput>();

            _dataTable = DataTableProvider.Instance.GetSharedDataTable(_entity.Descriptor.Value.TableName);

            var finder = _finderProvider.CreateFinder(compareFieldSelector, resultFieldSelector, _entity.Comparer.Value);
            var rows = _dataTable.Select();

            foreach (DataRow row in rows)
            {
                var findResult = Activator.CreateInstance<TInput>();

                foreach (DataColumn column in _dataTable.Columns)
                {
                    var value = row[column.ColumnName];
                    if (value is not DBNull)
                    {
                        var mapper = _entity.Accessor.Value.GetValueMapper(_entity.Descriptor.Value.GetField(column.ColumnName));
                        mapper(value, findResult);
                    }
                }

                if (findEntities.Any(x => finder.Compare(findResult, x)))
                { 
                    results.Add(findResult);
                }
            }

            return Task.FromResult(results.AsEnumerable());
        }

        public Task<int> InsertAsync(IEnumerable<TInput> data)
        {
            _dataTable = DataTableProvider.Instance.GetSharedDataTable(_entity.Descriptor.Value.TableName);
            using (_sharedLockManager.GetSharedWriteLock(_entity.Descriptor.Value.TableName))
            {
                CreateColumnsIfNotExists();

                var rowsChanged = 0;

                foreach (var item in data)
                {
                    var row = _dataTable.NewRow();
                    foreach (var action in _dataRowValueAssigningActions)
                    {
                        action.Value.Invoke(row, item);
                    }
                    _dataTable.Rows.Add(row);
                    rowsChanged++;
                }

                return Task.FromResult(rowsChanged);
            }
        }

        public Task<int> UpdateAsync(IEnumerable<RowUpdate<TInput>> data)
        {
            return Task.FromResult(0);
        }

        private bool CreateColumnsIfNotExists()
        {
            _dataTable = DataTableProvider.Instance.GetSharedDataTable(_entity.Descriptor.Value.TableName);
            if (_dataTable.Columns.Count > 0)
            {
                return false;
            }

            foreach (var field in _entity.Descriptor.Value.Fields)
            {
                var column = new DataColumn();
                column.ColumnName = field.Name;
                column.DataType = Nullable.GetUnderlyingType(field.Property.PropertyType) ?? field.Property.PropertyType;
                column.AllowDBNull = Nullable.GetUnderlyingType(field.Property.PropertyType) is not null;
                if (field.IsKey && field.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity)
                {
                    column.AutoIncrement = true;
                    column.AutoIncrementSeed = 1;
                    column.AutoIncrementStep = 1;
                }

                _dataTable.Columns.Add(column);
            }

            return true;
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
    }
}
