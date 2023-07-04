using CsvHelper;
using Microsoft.Extensions.Logging;
using Rowbot.Framework.Blocks.Connectors.Synchronisation;
using System.Globalization;

namespace Rowbot.Connectors.Csv
{
    public sealed class CsvReadConnector<TSource> : IReadConnector<TSource, CsvConnectorOptions<TSource>>
    {
        private readonly ILogger<CsvReadConnector<TSource>> _logger;
        private readonly ISharedLockManager _sharedLockManager;

        public CsvReadConnector(ILogger<CsvReadConnector<TSource>> logger, ISharedLockManager sharedLockManager)
        {
            Options = new();
            _logger = logger;
            _sharedLockManager = sharedLockManager;
        }

        public CsvConnectorOptions<TSource> Options { get; set; }

        public async Task<IEnumerable<TSource>> QueryAsync(IEnumerable<ExtractParameter> parameters)
        {
            var result = new List<TSource>();
            var filter = Options.FilterExpression?.Compile();
            if (filter != null)
            {
                _logger.LogInformation("Query with filter {filter}", Options.FilterDescription);
            }

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
                    var record = csv.GetRecord<TSource>();
                    if (filter is null || filter(record))
                    {
                        result.Add(record);
                    }
                }
            }

            return result;
        }
    }
}
