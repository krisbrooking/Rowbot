using Microsoft.Extensions.Logging;
using Rowbot.Common.Services;
using Rowbot.Connectors.Null;
using Rowbot.Entities;
using Rowbot.Entities.DataAnnotations;
using Rowbot.Framework.Blocks.Loaders;
using Rowbot.Loaders.SlowlyChangingDimensions;
using System.Reflection;
using System.Transactions;

namespace Rowbot
{
    public sealed class SlowlyChangingDimensionLoader<TTarget> : ILoader<TTarget, SlowlyChangingDimensionLoaderOptions<TTarget>>
        where TTarget : Dimension
    {
        private readonly ILogger<SlowlyChangingDimensionLoader<TTarget>> _logger;
        private readonly IEntity<TTarget> _entity;
        private readonly ISystemClock _systemClock;
        private readonly List<string> _propertiesToIgnore;

        public SlowlyChangingDimensionLoader(
            ILogger<SlowlyChangingDimensionLoader<TTarget>> logger,
            IEntity<TTarget> entity,
            ISystemClock systemClock)
        {
            Options = new();
            Connector = new NullWriteConnector<TTarget>();
            _logger = logger;
            _entity = entity;
            _systemClock = systemClock;
            _propertiesToIgnore = new List<string>()
            {
                nameof(Row.KeyHash),
                nameof(Row.ChangeHash),
                nameof(Row.IsDeleted),
                nameof(Row.KeyHashBase64),
                nameof(Row.ChangeHashBase64),
                nameof(Fact.Created),
                nameof(Dimension.FromDateKey),
                nameof(Dimension.FromDate),
                nameof(Dimension.ToDate),
                nameof(Dimension.ToDateKey),
                nameof(Dimension.IsActive)
            };
        }

        public SlowlyChangingDimensionLoaderOptions<TTarget> Options { get; set; }
        public IWriteConnector<TTarget> Connector { get; set; }

        public async Task<LoadResult<TTarget>> LoadAsync(TTarget[] data)
        {
            var keyField = _entity.Descriptor.Value.KeyFields.Single();

            var findResults = await Connector.FindAsync(data, compare => compare.Include(x => x.KeyHash).Include(x => x.IsActive), result => result.All());
            var findResultsMap = findResults
                .GroupBy(x => x.KeyHashBase64, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
            var keyAccessor = _entity.Accessor.Value.GetValueAccessor(keyField);
            var keyMapper = _entity.Accessor.Value.GetValueMapper(keyField);

            return await ApplySlowlyChangingDimensions(data, findResultsMap, keyAccessor, keyMapper);
        }

        private async Task<LoadResult<TTarget>> ApplySlowlyChangingDimensions(
            IEnumerable<TTarget> rows,
            Dictionary<string, TTarget> findResultsMap,
            Func<TTarget, object> keyAccessor,
            Action<object, TTarget> keyMapper)
        {
            var rowsToInsert = new List<TTarget>();
            var rowsToUpdate = new List<Update<TTarget>>();
            var type2RowsToInsert = new List<TTarget>();
            var type2RowsToUpdate = new List<Update<TTarget>>();

            var hashCodesProcessed = new HashSet<string>();

            foreach (var row in rows)
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

                if (row.IsDeleted && findResult.ToDate is null)
                {
                    var update = DeleteRow(findResult, row, key);
                    rowsToUpdate.Add(update);

                    continue;
                }

                if (!row.IsDeleted && findResult.IsDeleted)
                {
                    var update = UndeleteRow(findResult, key);
                    rowsToUpdate.Add(update);
                }

                if (!row.ChangeHash.SequenceEqual(findResult.ChangeHash))
                {
                    var changedProperties = GetChangedProperties(row, findResult);
                    if (changedProperties.All(x => _propertiesToIgnore.Contains(x.Property.Name)))
                    {
                        continue;
                    }

                    var type2Properties = GetType2Properties(_entity);

                    if (GetType2PropertiesChanged(changedProperties, type2Properties).Any())
                    {
                        _logger.LogTrace("Updating (Type 2) {key} {keyhash} {changes}", key.ToString(), findResult.KeyHashBase64, string.Join(',', changedProperties.Select(x => x.Name)));

                        findResult.IsActive = false;
                        findResult.ToDate = _systemClock.UtcNow;

                        type2RowsToUpdate.Add(new Update<TTarget>(findResult, 
                            _entity.Descriptor.Value.GetField(nameof(Dimension.IsActive)), 
                            _entity.Descriptor.Value.GetField(nameof(Dimension.ToDate)), 
                            _entity.Descriptor.Value.GetField(nameof(Dimension.ToDateKey))));

                        type2RowsToInsert.Add(row);

                        continue;
                    }

                    if (changedProperties.Any())
                    {
                        _logger.LogTrace("Updating {key} {keyhash} {changes}", key.ToString(), findResult.KeyHashBase64, string.Join(',', changedProperties.Select(x => x.Name)));

                        keyMapper(key, row);
                        rowsToUpdate.Add(new Update<TTarget>(row, changedProperties));
                    }
                }
            }

            return await ApplyInsertsAndUpdatesAsync(rowsToInsert, rowsToUpdate, type2RowsToInsert, type2RowsToUpdate);
        }

        private async Task<LoadResult<TTarget>> ApplyInsertsAndUpdatesAsync(
            List<TTarget> rowsToInsert, 
            List<Update<TTarget>> rowsToUpdate,
            List<TTarget> type2RowsToInsert,
            List<Update<TTarget>> type2RowsToUpdate)
        {
            if (type2RowsToUpdate.Count > 0)
            {
                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    var rowsUpdated = await Connector.UpdateAsync(type2RowsToUpdate);
                    var rowsInserted = await Connector.InsertAsync(type2RowsToInsert);

                    scope.Complete();

                    _logger.LogInformation("Type 2 rows updated: {rows}/{total}", rowsUpdated, type2RowsToUpdate.Count);
                    _logger.LogInformation("Type 2 rows inserted: {rows}/{total}", rowsInserted, type2RowsToInsert.Count);
                }
            }

            if (rowsToInsert.Count > 0)
            {
                var rowsInserted = await Connector.InsertAsync(rowsToInsert);
                _logger.LogInformation("Rows inserted: {rows}/{total}", rowsInserted, rowsToInsert.Count);
            }
            if (rowsToUpdate.Count > 0)
            {
                var rowsUpdated = await Connector.UpdateAsync(rowsToUpdate);
                _logger.LogInformation("Rows updated: {rows}/{total}", rowsUpdated, rowsToUpdate.Count);
            }

            if (type2RowsToUpdate.Count == 0 &&
                rowsToInsert.Count == 0 &&
                rowsToUpdate.Count == 0)
            {
                _logger.LogInformation("Rows changed: 0");
            }

            return new LoadResult<TTarget>(rowsToInsert.Concat(type2RowsToInsert), rowsToUpdate.Concat(type2RowsToUpdate));
        }

        private Update<TTarget> DeleteRow(TTarget findResult, TTarget row, object key)
        {
            if (Options.OverrideDeleteWithIsActiveFalse)
            {
                findResult.IsActive = false;
            }
            else
            {
                findResult.IsDeleted = true;
            }
            findResult.ToDate = _systemClock.UtcNow;

            var changedProperties = new List<FieldDescriptor>()
                    {
                        _entity.Descriptor.Value.GetField(Options.OverrideDeleteWithIsActiveFalse ? nameof(Dimension.IsActive) : nameof(Row.IsDeleted)),
                        _entity.Descriptor.Value.GetField(nameof(Dimension.ToDate)),
                        _entity.Descriptor.Value.GetField(nameof(Dimension.ToDateKey))
                    };

            if (Options.FieldsToUpdateOnDelete is { })
            {
                foreach (var field in Options.FieldsToUpdateOnDelete.Selected)
                {
                    var accessor = _entity.Accessor.Value.GetValueAccessor(field);
                    var mapper = _entity.Accessor.Value.GetValueMapper(field);
                    mapper(accessor(row), findResult);

                    changedProperties.Add(field);
                }
            }

            _logger.LogTrace("Deleting {key} {keyhash}", key.ToString(), findResult.KeyHashBase64);

            return new Update<TTarget>(findResult, changedProperties);
        }

        private Update<TTarget> UndeleteRow(TTarget findResult, object key)
        {
            findResult.IsDeleted = false;
            findResult.ToDate = null;

            var changedProperties = new List<FieldDescriptor>()
            {
                _entity.Descriptor.Value.GetField(nameof(Row.IsDeleted)),
                _entity.Descriptor.Value.GetField(nameof(Dimension.ToDate)),
                _entity.Descriptor.Value.GetField(nameof(Dimension.ToDateKey))
            };

            _logger.LogTrace("Undeleting {key} {keyhash}", key.ToString(), findResult.KeyHashBase64);

            return new Update<TTarget>(findResult, changedProperties);
        }

        private List<FieldDescriptor> GetChangedProperties(TTarget row, TTarget findResult)
        {
            var changedProperties = new List<FieldDescriptor>();

            foreach (var field in _entity.Descriptor.Value.Fields
                .Where(x =>
                    x.Name != nameof(Row.KeyHash) &&
                    x.Name != nameof(Dimension.FromDate) &&
                    x.Name != nameof(Dimension.FromDateKey) &&
                    x.Name != nameof(Dimension.ToDate) &&
                    x.Name != nameof(Dimension.ToDateKey))
                .Where(x => !x.IsKey))
            {
                if (!_entity.Comparer.Value.FieldEquals(field, row, findResult))
                {
                    changedProperties.Add(field);
                }
            }

            return changedProperties;
        }

        private HashSet<string> GetType2Properties(IEntity<TTarget> entity)
            =>
            entity.Descriptor.Value.Fields
                .Where(x => !x.IsKey)
                .Where(x => x.Property.GetCustomAttribute<SlowlyChangingDimensionType2Attribute>() is not null)
                .Select(x => x.Name)
                .ToHashSet();

        private HashSet<string> GetType2PropertiesChanged(List<FieldDescriptor> changedProperties, HashSet<string> type2Properties)
            =>
            type2Properties
                .Where(x => changedProperties.Any(c => string.Equals(c.Property.Name, x, StringComparison.InvariantCultureIgnoreCase)))
                .ToHashSet();
    }
}
