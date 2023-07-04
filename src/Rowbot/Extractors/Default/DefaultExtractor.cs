using Rowbot.Connectors.Null;
using System.Runtime.CompilerServices;

namespace Rowbot.Extractors.Default
{
    public class DefaultExtractor<TSource> : IExtractor<TSource, DefaultExtractorOptions<TSource>>
    {
        public DefaultExtractor()
        {
            Options = new();
            Connector = new NullReadConnector<TSource>();
        }

        public DefaultExtractorOptions<TSource> Options { get; set; }
        public IReadConnector<TSource> Connector { get; set; }

        public async IAsyncEnumerable<TSource> ExtractAsync(ExtractParameterCollection userDefinedParameters, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            var queryResult = await Connector!.QueryAsync(userDefinedParameters);

            foreach (var item in queryResult)
            {
                yield return item;
            }
        }
    }
}
