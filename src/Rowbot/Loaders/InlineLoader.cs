using Rowbot.Loaders.Framework;

namespace Rowbot.Loaders;

public sealed class InlineLoader<TInput> : ILoader<TInput>
{
    public IWriteConnector<TInput>? Connector { get; set; }
    public Func<TInput[], IWriteConnector<TInput>, Task<LoadResult<TInput>>> LoaderDelegate { get; set; }
        = (data, connector) => Task.FromResult(new LoadResult<TInput>([], []));

    public async Task<LoadResult<TInput>> LoadAsync(TInput[] data)
    {
        if (Connector is null)
        {
            throw new InvalidOperationException("Write connector is not configured");
        }

        return await LoaderDelegate(data, Connector!);
    }
}