using Microsoft.Extensions.Logging;
using Rowbot.Connectors.Null;
using Rowbot.Entities;
using Rowbot.Framework.Blocks.Loaders;
using Rowbot.Loaders.SnapshotFacts;

namespace Rowbot
{
    public sealed class SnapshotFactLoader<TTarget> : ILoader<TTarget, SnapshotFactLoaderOptions<TTarget>>
        where TTarget : Fact
    {
        private readonly ILogger<SnapshotFactLoader<TTarget>> _logger;
        private readonly IEntity<TTarget> _entity;

        public SnapshotFactLoader(
            ILogger<SnapshotFactLoader<TTarget>> logger,
            IEntity<TTarget> entity)
        {
            Options = new();
            Connector = new NullWriteConnector<TTarget>();
            _logger = logger;
            _entity = entity;
        }

        public SnapshotFactLoaderOptions<TTarget> Options { get; set; }
        public IWriteConnector<TTarget> Connector { get; set; }

        public async Task<LoadResult<TTarget>> LoadAsync(TTarget[] data)
        {
            return await ApplyAccumulatingSnapshotFacts(Connector, data);
        }

        internal async Task<LoadResult<TTarget>> ApplyAccumulatingSnapshotFacts(IWriteConnector<TTarget> connector, IEnumerable<TTarget> rows)
        {
            var keyField = _entity.Descriptor.Value.KeyFields.Single();

            var findResults = await connector.FindAsync(rows, compare => compare.Include(x => x.KeyHash), result => result.All());
            var keyAccessor = _entity.Accessor.Value.GetValueAccessor(keyField);
            var keyMapper = _entity.Accessor.Value.GetValueMapper(keyField);

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

                var findResult = findResults.FirstOrDefault(x => x.KeyHash.SequenceEqual(row.KeyHash));
                if (findResult is null)
                {
                    _logger.LogTrace("Inserting {keyhash}", row.KeyHashBase64);
                    rowsToInsert.Add(row);

                    continue;
                }

                var key = keyAccessor(findResult);
                if (key is null)
                {
                    _logger.LogError("Failed to get key for find result with key hash {keyHash}", findResult.KeyHashBase64);
                    continue;
                }

                if (row.IsDeleted)
                {
                    _logger.LogTrace("Deleting {key} {keyhash}", key.ToString(), findResult.KeyHashBase64);

                    findResult.IsDeleted = true;
                    var update = new Update<TTarget>(findResult, _entity.Descriptor.Value.GetField(nameof(Row.IsDeleted)));
                    rowsToUpdate.Add(update);

                    continue;
                }

                if (!row.IsDeleted && findResult.IsDeleted)
                {
                    _logger.LogTrace("Undeleting {key} {keyhash}", key.ToString(), findResult.KeyHashBase64);

                    findResult.IsDeleted = false;
                    rowsToUpdate.Add(new Update<TTarget>(findResult, _entity.Descriptor.Value.GetField(nameof(Row.IsDeleted))));

                    continue;
                }

                if (!row.ChangeHash.SequenceEqual(findResult.ChangeHash))
                {
                    var changedProperties = GetChangedProperties(row, findResult);

                    _logger.LogTrace("Updating {key} {keyhash} {changes}", key.ToString(), findResult.KeyHashBase64, string.Join(',', changedProperties.Select(x => x.Name)));

                    keyMapper(key, row);
                    rowsToUpdate.Add(new Update<TTarget>(row, changedProperties));

                    continue;
                }
            }

            var rowsInserted = await connector.InsertAsync(rowsToInsert);
            _logger.LogInformation("Rows inserted: {rows}/{total}", rowsInserted, rowsToInsert.Count);

            var rowsUpdated = await connector.UpdateAsync(rowsToUpdate);
            _logger.LogInformation("Rows updated: {rows}/{total}", rowsUpdated, rowsToUpdate.Count);

            return new LoadResult<TTarget>(rowsToInsert, rowsToUpdate);
        }

        private List<FieldDescriptor> GetChangedProperties(TTarget row, TTarget findResult)
        {
            var changedProperties = new List<FieldDescriptor>();

            foreach (var field in _entity.Descriptor.Value.Fields
                .Where(x => x.Name != nameof(Fact.Created))
                .Where(x => !x.IsKey))
            {
                if (!_entity.Comparer.Value.FieldEquals(field, row, findResult))
                {
                    changedProperties.Add(field);
                }
            }

            return changedProperties;
        }
    }
}
