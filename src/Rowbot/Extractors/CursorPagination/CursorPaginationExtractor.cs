using System.Runtime.CompilerServices;
using Rowbot.Extractors.Framework;

namespace Rowbot.Extractors.CursorPagination;

public class CursorPaginationExtractor<TInput, TOutput, TCursor> : IExtractor<TInput, TOutput>
{
    public IReadConnector<TInput, TOutput>? Connector { get; set; }
    public CursorPaginationOptions<TOutput, TCursor> Options { get; set; } = new();

    public async IAsyncEnumerable<TOutput> ExtractAsync(ExtractContext<TInput> context, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (Connector is null)
        {
            throw new InvalidOperationException("Read connector is not configured");
        }
            
        var dataPager = new CursorDataPager<TOutput, TCursor>(Options.Cursor, Options.InitialValue, Options.OrderBy);

        var dataPagerParameters = dataPager.Next();

        while (!dataPager.IsEndOfQuery && !cancellationToken.IsCancellationRequested)
        {
            var queryResult = await Connector.QueryAsync([..context.GetParameters(), ..dataPagerParameters]);

            foreach (var item in queryResult)
            {
                dataPager.AddResults(item);
                yield return item;
            }

            var nextDataPagerParameters = dataPager.Next();

            if (nextDataPagerParameters.SequenceEqual(dataPagerParameters))
            {
                throw new InvalidOperationException("Data pager parameters have not changed since the last iteration");
            }
                
            dataPagerParameters = nextDataPagerParameters;
        }
    }
}