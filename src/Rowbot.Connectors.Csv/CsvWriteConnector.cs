using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Rowbot.Entities;
using Rowbot.Connectors.Common.Find;
using Rowbot.Connectors.Common.Synchronisation;
using System.Globalization;

namespace Rowbot.Connectors.Csv
{
    public sealed class CsvWriteConnector<TInput>(
        IEntity<TInput> entity, 
        ILogger<CsvWriteConnector<TInput>> logger, 
        IFinderProvider finderProvider, 
        ISharedLockManager sharedLockManager) : IWriteConnector<TInput>
    {
        private readonly IEntity<TInput> _entity = entity;
        private readonly ILogger<CsvWriteConnector<TInput>> _logger = logger;
        private readonly IFinderProvider _finderProvider = finderProvider;
        private readonly ISharedLockManager _sharedLockManager = sharedLockManager;

        public CsvConnectorOptions<TInput> Options { get; set; } = new();

        public async Task<IEnumerable<TInput>> FindAsync(
            IEnumerable<TInput> findEntities, 
            Action<IFieldSelector<TInput>> compareFieldsSelector, 
            Action<IFieldSelector<TInput>> resultFieldsSelector)
        {
            var result = new List<TInput>();
            var finder = _finderProvider.CreateFinder(compareFieldsSelector, resultFieldsSelector, _entity.Comparer.Value);

            using (_sharedLockManager.GetSharedReadLock(Options.FilePath))
            using (var reader = new StreamReader(Options.FilePath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                await csv.ReadAsync();
                if (Options.HasHeaderRow)
                {
                    csv.ReadHeader();
                }
                while (await csv.ReadAsync())
                {
                    var record = csv.GetRecord<TInput>();
                    if (findEntities.Any(x => finder.Compare(record, x)))
                    {
                        result.Add(finder.Return(record));
                    }
                }
            }

            return result;
        }

        public async Task<int> InsertAsync(IEnumerable<TInput> data)
        {
            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture);

            var classMap = new DefaultClassMap<TInput>();
            classMap.AutoMap(CultureInfo.InvariantCulture);

            foreach (var fieldDescriptor in _entity.Descriptor.Value.IgnoredFields)
            {
                classMap.Map(typeof(TInput), fieldDescriptor.Property).Ignore();
            }

            foreach (var fieldDescriptor in _entity.Descriptor.Value.Fields)
            {
                if (!string.Equals(fieldDescriptor.Name, fieldDescriptor.Property.Name, StringComparison.OrdinalIgnoreCase))
                {
                    classMap.Map(typeof(TInput), fieldDescriptor.Property).Name(fieldDescriptor.Name);
                }
            }

            using (_sharedLockManager.GetSharedWriteLock(Options.FilePath))
            using (var writer = new StreamWriter(Options.FilePath, append: true))
            using (var csv = new CsvWriter(writer, configuration))
            {

                csv.Context.RegisterClassMap(classMap);
                await csv.WriteRecordsAsync(data);
                return data.Count();
            }
        }

        public Task<int> UpdateAsync(IEnumerable<RowUpdate<TInput>> data)
        {
            return InsertAsync(data.Select(x => x.Row));
        }
    }
}
