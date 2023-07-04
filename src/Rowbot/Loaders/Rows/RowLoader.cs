using Microsoft.Extensions.Logging;
using Rowbot.Connectors.Null;
using Rowbot.Framework.Blocks.Loaders;
using Rowbot.Loaders.Rows;

namespace Rowbot
{
    public sealed class RowLoader<TTarget> : ILoader<TTarget, RowLoaderOptions<TTarget>>
    {
        private readonly ILogger<RowLoader<TTarget>> _logger;

        public RowLoader(
            ILogger<RowLoader<TTarget>> logger)
        {
            Options = new();
            Connector = new NullWriteConnector<TTarget>();
            _logger = logger;
        }

        public RowLoaderOptions<TTarget> Options { get; set; }
        public IWriteConnector<TTarget> Connector { get; set; }

        public async Task<LoadResult<TTarget>> LoadAsync(TTarget[] data)
        {
            return await ApplyRows(Connector, data);
        }

        internal async Task<LoadResult<TTarget>> ApplyRows(IWriteConnector<TTarget> connector, IEnumerable<TTarget> rows)
        {
            var rowsToInsert = rows.ToArray();
            var rowsInserted = await connector.InsertAsync(rows);
            _logger.LogInformation("Rows inserted: {rows}/{total}", rowsInserted, rowsToInsert.Length);

            return new LoadResult<TTarget>(rows, Enumerable.Empty<Update<TTarget>>());
        }
    }
}
