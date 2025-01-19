using Microsoft.Extensions.Logging;
using Rowbot.Loaders.Framework;
using Rowbot.Entities;

namespace Rowbot.Loaders.TransactionFact;

public sealed class TransactionFactLoader<TInput>(
    ILogger<TransactionFactLoader<TInput>> logger,
    IEntity<TInput> entity) : ILoader<TInput>
    where TInput : Fact
{
    private readonly ILogger<TransactionFactLoader<TInput>> _logger = logger;
    private readonly IEntity<TInput> _entity = entity;

    public IWriteConnector<TInput>? Connector { get; set; }

    public async Task<LoadResult<TInput>> LoadAsync(TInput[] data)
    {
        if (Connector is null)
        {
            throw new InvalidOperationException("Write connector is not configured");
        }
        
        return await ApplyFacts(Connector, data);
    }

    internal async Task<LoadResult<TInput>> ApplyFacts(IWriteConnector<TInput> connector, IEnumerable<TInput> rows)
    {
        var keyField = _entity.Descriptor.Value.KeyFields.Single();

        var findResults = await connector.FindAsync(rows, compare => compare.Include(x => x.KeyHash), result => result.All());
        var findResultsMap = findResults
            .GroupBy(x => x.KeyHashBase64, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

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
            }
        }

        if (rowsToInsert.Count > 0)
        {
            var rowsInserted = await connector.InsertAsync(rowsToInsert);
            _logger.LogInformation("Rows inserted: {rows}/{total}", rowsInserted, rowsToInsert.Count);
        }
        else
        {
            _logger.LogInformation("Rows changed: 0");
        }

        return new LoadResult<TInput>(rowsToInsert, Enumerable.Empty<RowUpdate<TInput>>());
    }
}