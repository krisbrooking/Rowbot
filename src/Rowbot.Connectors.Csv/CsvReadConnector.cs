using CsvHelper;
using Microsoft.Extensions.Logging;
using Rowbot.Connectors.Common.Synchronisation;
using System.Globalization;

namespace Rowbot.Connectors.Csv
{
    public sealed class CsvReadConnector<TInput, TOutput>(
        ILogger<CsvReadConnector<TInput, TOutput>> logger, 
        ISharedLockManager sharedLockManager) : IReadConnector<TInput, TOutput>
    {
        private readonly ILogger<CsvReadConnector<TInput, TOutput>> _logger = logger;
        private readonly ISharedLockManager _sharedLockManager = sharedLockManager;

        public CsvConnectorOptions<TOutput> Options { get; set; } = new();

        public async Task<IEnumerable<TOutput>> QueryAsync(ExtractParameter[] parameters)
        {
            var result = new List<TOutput>();
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
                    var record = csv.GetRecord<TOutput>();
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
