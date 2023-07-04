using Microsoft.Extensions.Logging;

namespace Rowbot.Framework.Pipelines.Builder
{
    /// <summary>
    /// Shared data passed between pipeline builder stages <see cref="PipelineBuilder"/>, <see cref="PipelineExtractor{TSource}"/>, 
    /// <see cref="PipelineTransformer{TPrevious, TSource}"/>, and <see cref="PipelineLoader{TTarget}"/>
    /// </summary>
    /// <remarks>
    /// The pipeline builder stages after <see cref="PipelineBuilder"/> don't support dependency injection 
    /// so dependencies are injected in <see cref="PipelineBuilder"/> and passed between stages using
    /// <see cref="PipelineBuilderContext"/>
    /// </remarks>
    public sealed class PipelineBuilderContext
    {
        public PipelineBuilderContext(ILoggerFactory loggerFactory, ServiceFactory genericServiceFactory)
        {
            LoggerFactory = loggerFactory;
            ServiceFactory = genericServiceFactory;
        }

        public ILoggerFactory LoggerFactory { get; }
        public ServiceFactory ServiceFactory { get; }
        /// <summary>
        /// Keeps track of all data required to build a pipeline as pipeline builder stages are invoked.
        /// </summary>
        public PipelineDefinition Definition { get; private set; } = new();
        /// <summary>
        /// Creates a new instance of <see cref="PipelineDefinition"/>. This allows a single instance of
        /// <see cref="PipelineBuilder"/> to create multiple pipelines.
        /// </summary>
        public void ResetDefinition() => Definition = new();
    }
}
