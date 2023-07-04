using Rowbot.Connectors.Null;
using Rowbot.Extractors.CursorPagination;
using System.Runtime.CompilerServices;

namespace Rowbot
{
    public class CursorPaginationExtractor<TSource> : IExtractor<TSource, CursorPaginationExtractorOptions<TSource>>
    {
        public CursorPaginationExtractor()
        {
            Options = new();
            Connector = new NullReadConnector<TSource>();
        }

        public CursorPaginationExtractorOptions<TSource> Options { get; set; }
        public IReadConnector<TSource> Connector { get; set; }

        public async IAsyncEnumerable<TSource> ExtractAsync(ExtractParameterCollection userDefinedParameters, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var dataPager = Options.DataPagerFactory();

            var dataPagerParameters = dataPager.Next();

            while (!dataPager.IsEndOfQuery && !cancellationToken.IsCancellationRequested)
            {
                var extractParameters = dataPagerParameters.Concat(userDefinedParameters);

                var queryResult = await Connector!.QueryAsync(extractParameters);

                foreach (var item in queryResult)
                {
                    dataPager.AddResults(item);
                    yield return item;
                }

                dataPagerParameters = dataPager.Next();
            }
        }
    }
}
