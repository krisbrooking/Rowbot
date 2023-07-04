using Microsoft.Extensions.Logging;
using Rowbot.Framework.Blocks.Connectors.Synchronisation;

namespace Rowbot.Connectors.Json
{
    public class JsonReadConnector<TSource> : IReadConnector<TSource, JsonReadConnectorOptions<TSource>>
    {
        private readonly ILogger<JsonReadConnector<TSource>> _logger;
        private readonly ISharedLockManager _sharedLockManager;

        public JsonReadConnector(ILogger<JsonReadConnector<TSource>> logger, ISharedLockManager sharedLockManager)
        {
            Options = new();
            _logger = logger;
            _sharedLockManager = sharedLockManager;
        }

        public JsonReadConnectorOptions<TSource> Options { get; set; }

        public Task<IEnumerable<TSource>> QueryAsync(IEnumerable<ExtractParameter> parameters)
        {
            var result = new List<TSource>();
            var filter = Options.FilterExpression?.Compile();
            if (filter != null)
            {
                _logger.LogInformation("Query with filter {filter}", Options.FilterDescription);
            }

            using (_sharedLockManager.GetSharedReadLock(Options.FilePath))
            using (var stream = new FileStream(Options.FilePath, FileMode.Open))
            using (var reader = new JsonStreamReader(stream))
            {
                foreach (var record in reader.GetRecords<TSource>())
                {
                    if (filter is null || filter(record))
                    {
                        result.Add(record);
                    }
                }
            }

            return Task.FromResult(result.AsEnumerable());
        }
    }
}
