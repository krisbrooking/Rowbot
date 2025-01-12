using System.Runtime.CompilerServices;
using Rowbot.Extractors.Framework;

namespace Rowbot.Extractors;

public sealed class InlineExtractor<TInput, TOutput> : IExtractor<TInput, TOutput>
{
    public IReadConnector<TInput, TOutput>? Connector { get; set; }
    public Func<ExtractContext<TInput>, IReadConnector<TInput, TOutput>, Task<IEnumerable<TOutput>>> ExtractorDelegate { get; set; }
        = (context, connector) => Task.FromResult<IEnumerable<TOutput>>(Array.Empty<TOutput>());

    public async IAsyncEnumerable<TOutput> ExtractAsync(ExtractContext<TInput> context, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (Connector is null)
        {
            throw new InvalidOperationException("Read connector is not configured");
        }
            
        if (cancellationToken.IsCancellationRequested)
        {
            yield break;
        }

        var queryResult = await ExtractorDelegate(context, Connector!).ConfigureAwait(false);

        foreach (var item in queryResult)
        {
            yield return item;
        }
    }
}