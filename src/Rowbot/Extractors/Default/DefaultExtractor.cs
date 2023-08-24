using Rowbot.Connectors.Null;
using Rowbot.Framework.Pipelines.Options;
using System.Runtime.CompilerServices;

namespace Rowbot.Extractors.Default
{
    public class DefaultExtractor<TSource> : IExtractor<TSource, ExtractorOptions>
    {
        public DefaultExtractor()
        {
            Options = new();
            Connector = new NullReadConnector<TSource>();
        }

        public ExtractorOptions Options { get; set; }
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
