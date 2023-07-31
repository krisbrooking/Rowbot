using Microsoft.Extensions.Logging;
using Rowbot.Connectors.Null;
using Rowbot.Entities;
using Rowbot.Framework.Blocks.Loaders;
using Rowbot.Loaders.Facts;

namespace Rowbot
{
    public sealed class FactLoader<TTarget> : ILoader<TTarget, FactLoaderOptions<TTarget>>
        where TTarget : Fact
    {
        private readonly ILogger<FactLoader<TTarget>> _logger;
        private readonly IEntity<TTarget> _entity;

        public FactLoader(
            ILogger<FactLoader<TTarget>> logger,
            IEntity<TTarget> entity)
        {
            Options = new();
            Connector = new NullWriteConnector<TTarget>();
            _logger = logger;
            _entity = entity;
        }

        public FactLoaderOptions<TTarget> Options { get; set; }
        public IWriteConnector<TTarget> Connector { get; set; }

        public async Task<LoadResult<TTarget>> LoadAsync(TTarget[] data)
        {
            return await ApplyFacts(Connector, data);
        }

        internal async Task<LoadResult<TTarget>> ApplyFacts(IWriteConnector<TTarget> connector, IEnumerable<TTarget> rows)
        {
            var keyField = _entity.Descriptor.Value.KeyFields.Single();

            var findResults = await connector.FindAsync(rows, compare => compare.Include(x => x.KeyHash), result => result.All());
            var findResultsMap = findResults
                .GroupBy(x => x.KeyHashBase64, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

            var rowsToInsert = new List<TTarget>();
            var rowsToUpdate = new List<Update<TTarget>>();

            var hashCodesProcessed = new HashSet<string>();

            foreach (var row in rows.Where(x => !hashCodesProcessed.Contains(x.KeyHashBase64)))
            {
                if (hashCodesProcessed.Contains(row.KeyHashBase64))
                {
                    _logger.LogWarning("Key hash {keyHash} already processed.", row.KeyHashBase64);
                    continue;
                }
                hashCodesProcessed.Add(row.KeyHashBase64);

                if (!findResultsMap.ContainsKey(row.KeyHashBase64))
                {
                    _logger.LogTrace("Inserting {keyhash}", row.KeyHashBase64);
                    rowsToInsert.Add(row);
                }
            }

            var rowsInserted = await connector.InsertAsync(rowsToInsert);
            _logger.LogInformation("Rows inserted: {rows}/{total}", rowsInserted, rowsToInsert.Count);

            return new LoadResult<TTarget>(rowsToInsert, Enumerable.Empty<Update<TTarget>>());
        }
    }
}
