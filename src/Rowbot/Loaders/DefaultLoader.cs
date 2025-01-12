using Rowbot.Loaders.Framework;

namespace Rowbot.Loaders;

public sealed class DefaultLoader<TInput> : ILoader<TInput>
{
    public IWriteConnector<TInput>? Connector { get; set; }

    public async Task<LoadResult<TInput>> LoadAsync(TInput[] data)
    {
        if (Connector is null)
        {
            throw new InvalidOperationException("Write connector is not configured");
        }
            
        var rowsToInsert = data.ToArray();
        var rowsInserted = await Connector.InsertAsync(data);

        return new LoadResult<TInput>(data, []);
    }
}