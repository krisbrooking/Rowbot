using Rowbot.Framework.Pipelines.Options;

namespace Rowbot
{
    public static class PipelineBuilderExtensions
    {
        /// <summary>
        /// Include extractor options.
        /// </summary>
        public static IPipelineExtractor<TSource> IncludeOptions<TSource>(this IPipelineExtractor<TSource> pipelineExtractor, Action<ExtractOptions> configure)
        {
            if (pipelineExtractor is PipelineExtractor<TSource> extractor)
            {
                configure(extractor._context.Definition.BlockContext.ExtractOptions);
            }

            return pipelineExtractor;
        }

        /// <summary>
        /// Include loader options.
        /// </summary>
        public static IPipelineLoader<TTarget> IncludeOptions<TTarget>(this IPipelineLoader<TTarget> pipelineLoader, Action<LoadOptions> configure)
        {
            if (pipelineLoader is PipelineLoader<TTarget> loader)
            {
                configure(loader._context.Definition.BlockContext.LoadOptions);
            }

            return pipelineLoader;
        }
    }
}
