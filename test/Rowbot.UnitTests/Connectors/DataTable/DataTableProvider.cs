using System;
using System.Collections.Concurrent;

namespace Rowbot.UnitTests.Connectors.DataTable
{
    public sealed class DataTableProvider
    {
        private static readonly Lazy<DataTableProvider> _lazy = new Lazy<DataTableProvider>(() => new DataTableProvider());
        private readonly ConcurrentDictionary<string, Lazy<System.Data.DataTable>> _dataTables = new();

        public System.Data.DataTable GetSharedDataTable(string name) =>
            _dataTables.GetOrAdd(name,
                x => new Lazy<System.Data.DataTable>(
                    () => new System.Data.DataTable(name)
                )
            ).Value;

        public bool Clear()
        {
            _dataTables.Clear();
            return _dataTables.Count == 0;
        }

        public bool Clear(string name)
        {
            Lazy<System.Data.DataTable>? dataTable;
            _dataTables.TryGetValue(name, out dataTable);
            if (dataTable is not null)
            {
                dataTable.Value.Clear();
            }
            
            return _dataTables.Count == 0;
        }

        public static DataTableProvider Instance => _lazy.Value;
    }
}
