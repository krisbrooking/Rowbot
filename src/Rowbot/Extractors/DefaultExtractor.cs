using System.Runtime.CompilerServices;
using Rowbot.Extractors.Framework;

namespace Rowbot.Extractors;

public class DefaultExtractor<TInput, TOutput> : IExtractor<TInput, TOutput>
{
    public IReadConnector<TInput, TOutput>? Connector { get; set; }

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

        var queryResult = await Connector!.QueryAsync(context.GetParameters());

        foreach (var item in queryResult)
        {
            yield return item;
        }
    }
}