using Microsoft.Extensions.Logging;
using Rowbot.Loaders.Framework;

namespace Rowbot.Loaders;

public sealed class DefaultLoader<TInput>(
    ILogger<DefaultLoader<TInput>> logger) : ILoader<TInput>
{
    private readonly ILogger<DefaultLoader<TInput>> _logger = logger;

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

        if (data.Count() > 0)
        {
            var rowsInserted = await Connector.InsertAsync(data);
            _logger.LogInformation("Rows inserted: {rows}/{total}", rowsInserted, data.Count());
        }
        else
        {
            _logger.LogInformation("Rows changed: 0");
        }

        return new LoadResult<TInput>(data, []);
    }
}