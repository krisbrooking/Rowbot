using Rowbot.Entities;
using Rowbot.Connectors.Common.Synchronisation;
using System.Data;

namespace Rowbot.UnitTests.Connectors.DataTable
{
    public sealed class DataTableReadConnector<TInput, TOutput>
        (IEntity<TOutput> entity, ISharedLockManager sharedLockManager) : IReadConnector<TInput, TOutput>
    {
        private readonly IEntity<TOutput> _entity = entity;
        private readonly ISharedLockManager _sharedLockManager = sharedLockManager;
        private System.Data.DataTable? _dataTable;

        public DataTableConnectorOptions<TOutput> Options { get; set; } = new();

        public Task<IEnumerable<TOutput>> QueryAsync(ExtractParameter[] parameters)
        {
            var result = new List<TOutput>();

            _dataTable = DataTableProvider.Instance.GetSharedDataTable(_entity.Descriptor.Value.TableName);
            using (_sharedLockManager.GetSharedReadLock(_entity.Descriptor.Value.TableName))
            {
                var rows = _dataTable.Select();
                foreach (DataRow row in rows)
                {
                    var rowResult = Activator.CreateInstance<TOutput>();
                    foreach (DataColumn column in _dataTable.Columns)
                    {
                        var field = _entity.Descriptor.Value.Fields.Single(x => string.Equals(x.Name, column.ColumnName, StringComparison.OrdinalIgnoreCase));
                        var value = row[column];

                        var mapper = _entity.Accessor.Value.GetValueMapper(field);
                        mapper(value, rowResult);
                    }

                    result.Add(rowResult);
                }
            }

            return Task.FromResult(result.AsEnumerable());
        }
    }
}
