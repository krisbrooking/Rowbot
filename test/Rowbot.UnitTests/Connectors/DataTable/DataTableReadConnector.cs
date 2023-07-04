using Rowbot.Entities;
using Rowbot.Framework.Blocks.Connectors.Synchronisation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Rowbot.UnitTests.Connectors.DataTable
{
    public interface IDataTableReadConnector<TSource> : IReadConnector<TSource, DataTableConnectorOptions<TSource>> { }

    public sealed class DataTableReadConnector<TSource> : IDataTableReadConnector<TSource>
    {
        private readonly IEntity<TSource> _entity;
        private readonly ISharedLockManager _sharedLockManager;
        private System.Data.DataTable? _dataTable;

        public DataTableReadConnector(IEntity<TSource> entity, ISharedLockManager sharedLockManager)
        {
            Options = new();
            _entity = entity;
            _sharedLockManager = sharedLockManager;
        }

        public DataTableConnectorOptions<TSource> Options { get; set; }

        public Task<IEnumerable<TSource>> QueryAsync(IEnumerable<ExtractParameter> parameters)
        {
            var result = new List<TSource>();

            _dataTable = DataTableProvider.Instance.GetSharedDataTable(_entity.Descriptor.Value.TableName);
            using (_sharedLockManager.GetSharedReadLock(_entity.Descriptor.Value.TableName))
            {
                var rows = _dataTable.Select();
                foreach (DataRow row in rows)
                {
                    var rowResult = Activator.CreateInstance<TSource>();
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
