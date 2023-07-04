using Rowbot.Connectors.Null;
using Rowbot.Extractors.OffsetPagination;
using System.Runtime.CompilerServices;

namespace Rowbot
{
    public class OffsetPaginationExtractor<TSource> : IExtractor<TSource, OffsetPaginationExtractorOptions<TSource>>
    {
        public OffsetPaginationExtractor()
        {
            Options = new();
            Connector = new NullReadConnector<TSource>();
        }

        public OffsetPaginationExtractorOptions<TSource> Options { get; set; }
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
