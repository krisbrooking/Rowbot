using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Rowbot.Entities;
using Rowbot.Framework.Blocks.Connectors.Find;
using Rowbot.Framework.Blocks.Connectors.Synchronisation;
using System.Globalization;

namespace Rowbot.Connectors.Csv
{
    public sealed class CsvWriteConnector<TEntity> : IWriteConnector<TEntity, CsvConnectorOptions<TEntity>>
    {
        private readonly IEntity<TEntity> _entity;
        private readonly ILogger<CsvWriteConnector<TEntity>> _logger;
        private readonly IFinderProvider _finderProvider;
        private readonly ISharedLockManager _sharedLockManager;

        public CsvWriteConnector(IEntity<TEntity> entity, ILogger<CsvWriteConnector<TEntity>> logger, IFinderProvider finderProvider, ISharedLockManager sharedLockManager)
        {
            Options = new();
            _logger = logger;
            _entity = entity;
            _finderProvider = finderProvider;
            _sharedLockManager = sharedLockManager;
        }

        public CsvConnectorOptions<TEntity> Options { get; set; }

        public async Task<IEnumerable<TEntity>> FindAsync(
            IEnumerable<TEntity> findEntities, 
            Action<IFieldSelector<TEntity>> compareFieldsSelector, 
            Action<IFieldSelector<TEntity>> resultFieldsSelector)
        {
            var result = new List<TEntity>();
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
                    var record = csv.GetRecord<TEntity>();
                    if (findEntities.Any(x => finder.Compare(record, x)))
                    {
                        result.Add(finder.Return(record));
                    }
                }
            }

            return result;
        }

        public async Task<int> InsertAsync(IEnumerable<TEntity> data)
        {
            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture);

            var classMap = new DefaultClassMap<TEntity>();
            classMap.AutoMap(CultureInfo.InvariantCulture);

            foreach (var fieldDescriptor in _entity.Descriptor.Value.IgnoredFields)
            {
                classMap.Map(typeof(TEntity), fieldDescriptor.Property).Ignore();
            }

            foreach (var fieldDescriptor in _entity.Descriptor.Value.Fields)
            {
                if (!string.Equals(fieldDescriptor.Name, fieldDescriptor.Property.Name, StringComparison.OrdinalIgnoreCase))
                {
                    classMap.Map(typeof(TEntity), fieldDescriptor.Property).Name(fieldDescriptor.Name);
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

        public Task<int> UpdateAsync(IEnumerable<Update<TEntity>> data)
        {
            return InsertAsync(data.Select(x => x.Row));
        }
    }
}
