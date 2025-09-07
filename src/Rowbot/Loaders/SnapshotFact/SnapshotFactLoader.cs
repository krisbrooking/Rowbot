using Microsoft.Extensions.Logging;
using Rowbot.Entities;
using Rowbot.Loaders.Framework;

namespace Rowbot.Loaders.SnapshotFact;

public sealed class SnapshotFactLoader<TInput>(
    ILogger<SnapshotFactLoader<TInput>> logger,
    IEntity<TInput> entity) : ILoader<TInput>
    where TInput : Fact
{
    private readonly ILogger<SnapshotFactLoader<TInput>> _logger = logger;
    private readonly IEntity<TInput> _entity = entity;

    public IWriteConnector<TInput>? Connector { get; set; }

    public async Task<LoadResult<TInput>> LoadAsync(TInput[] data, CancellationToken cancellationToken = default)
    {
        if (Connector is null)
        {
            throw new InvalidOperationException("Write connector is not configured");
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return new LoadResult<TInput>(Enumerable.Empty<TInput>(), Enumerable.Empty<RowUpdate<TInput>>());
        }

        return await ApplyAccumulatingSnapshotFacts(Connector, data);
    }

    internal async Task<LoadResult<TInput>> ApplyAccumulatingSnapshotFacts(IWriteConnector<TInput> connector, IEnumerable<TInput> rows)
    {
        var keyField = _entity.Descriptor.Value.KeyFields.Single();

        var findResults = await connector.FindAsync(rows, compare => compare.Include(x => x.KeyHash), result => result.All());
        var findResultsMap = findResults
            .GroupBy(x => x.KeyHashBase64, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        var keyAccessor = _entity.Accessor.Value.GetValueAccessor(keyField);
        var keyMapper = _entity.Accessor.Value.GetValueMapper(keyField);

        var rowsToInsert = new List<TInput>();
        var rowsToUpdate = new List<RowUpdate<TInput>>();

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

                continue;
            }
            var findResult = findResultsMap[row.KeyHashBase64];

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
                var update = new RowUpdate<TInput>(findResult, _entity.Descriptor.Value.GetField(nameof(Rowbot.Row.IsDeleted)));
                rowsToUpdate.Add(update);

                continue;
            }

            if (!row.IsDeleted && findResult.IsDeleted)
            {
                _logger.LogTrace("Undeleting {key} {keyhash}", key.ToString(), findResult.KeyHashBase64);

                findResult.IsDeleted = false;
                rowsToUpdate.Add(new RowUpdate<TInput>(findResult, _entity.Descriptor.Value.GetField(nameof(Rowbot.Row.IsDeleted))));

                continue;
            }

            if (!row.ChangeHash.SequenceEqual(findResult.ChangeHash))
            {
                var changedProperties = GetChangedProperties(row, findResult);

                _logger.LogTrace("Updating {key} {keyhash} {changes}", key.ToString(), findResult.KeyHashBase64, string.Join(',', changedProperties.Select(x => x.Name)));

                keyMapper(key, row);
                rowsToUpdate.Add(new RowUpdate<TInput>(row, changedProperties));

                continue;
            }
        }

        if (rowsToInsert.Count > 0)
        {
            var rowsInserted = await connector.InsertAsync(rowsToInsert);
            _logger.LogInformation("Rows inserted: {rows}/{total}", rowsInserted, rowsToInsert.Count);
        }

        if (rowsToUpdate.Count > 0)
        {
            var rowsUpdated = await connector.UpdateAsync(rowsToUpdate);
            _logger.LogInformation("Rows updated: {rows}/{total}", rowsUpdated, rowsToUpdate.Count);
        }

        if (rowsToInsert.Count == 0 && rowsToUpdate.Count == 0)
        {
            _logger.LogInformation("Rows changed: 0");
        }

        return new LoadResult<TInput>(rowsToInsert, rowsToUpdate);
    }

    private List<FieldDescriptor> GetChangedProperties(TInput row, TInput findResult)
    {
        var changedProperties = new List<FieldDescriptor>();

        foreach (var field in _entity.Descriptor.Value.Fields
            .Where(x => x.Name != nameof(Rowbot.Fact.Created))
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