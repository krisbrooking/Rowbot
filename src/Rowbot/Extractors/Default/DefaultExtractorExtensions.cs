using Rowbot.Extractors.Default;
using Rowbot.Framework.Pipelines.Options;

namespace Rowbot
{
    public static class DefaultExtractorExtensions
    {
        public static IPipelineTransformer<TSource, TSource> WithDefaultExtractor<TSource>(
            this IPipelineExtractor<TSource> pipelineExtractor,
            Action<ExtractorOptions>? configure = default)
        {
            var options = new ExtractorOptions();
            configure?.Invoke(options);

            return pipelineExtractor.WithExtractor<DefaultExtractor<TSource>, ExtractorOptions>(options);
        } 
    }
}