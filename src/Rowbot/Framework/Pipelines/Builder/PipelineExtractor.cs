using Rowbot.Extractors.Default;
using Rowbot.Framework.Blocks;
using Rowbot.Framework.Pipelines;
using Rowbot.Framework.Pipelines.Builder;
using Rowbot.Transformers.Default;

namespace Rowbot
{
    public interface ICustomPipelineExtractor<TSource> : ICustomPipeline
    {
        Func<IReadConnector<TSource>> ReadConnectorFactory { get; }
    }
    /// <summary>
    /// <see cref="IPipelineExtractor{TSource}"/> implements <see cref="IPipelineTransformer{TPrevious, TSource}"/> in order to provide a better
    /// exprience to the user when a custom extractor is not required. This implementation of <see cref="IPipelineTransformer{TPrevious, TSource}"/>
    /// automatically adds the default extractor into the pipeline.
    /// </summary>
    public interface IPipelineExtractor<TSource> : IPipelineTransformer<TSource, TSource>
    {
        /// <summary>
        /// Adds an extractor. <typeparamref name="TService"/> must be registered for dependency injection.
        /// </summary>
        /// <typeparam name="TService">An extractor that implements <see cref="IExtractor{TSource, TOptions}"/></typeparam>
        /// <typeparam name="TOptions">The options type of the extractor</typeparam>
        IPipelineTransformer<TSource, TSource> WithExtractor<TService, TOptions>(TOptions options)
            where TService : notnull, IExtractor<TSource, TOptions>;
    }

    public sealed class PipelineExtractor<TSource> : IPipelineExtractor<TSource>, ICustomPipelineExtractor<TSource>
    {
        internal readonly PipelineBuilderContext _context;
        private readonly Func<IReadConnector<TSource>> _readConnectorFactory;

        public PipelineExtractor(PipelineBuilderContext context, Func<IReadConnector<TSource>> readConnectorFactory)
        {
            _context = context;
            _readConnectorFactory = readConnectorFactory;
        }

        public IPipelineTransformer<TSource, TSource> WithExtractor<TService, TOptions>(TOptions options)
            where TService : notnull, IExtractor<TSource, TOptions>
        {
            var extractor = _context.ServiceFactory.CreateExtractor<TService, TSource, TOptions>(options, _readConnectorFactory());

            var sourceBlock = new ExtractBlock<TSource>(extractor, _context.LoggerFactory);
            _context.Definition.Blocks.Enqueue(sourceBlock, _context.Definition.Blocks.Count + 100);

            return new PipelineTransformer<TSource, TSource>(_context);
        }

        public IPipelineTransformer<TSource, TTarget> AddTransformer<TService, TTarget, TOptions>(TOptions options)
            where TService : notnull, ITransformer<TSource, TTarget, TOptions>
        {
            QueueDefaultExtractor();

            var transformer = _context.ServiceFactory.CreateTransformer<TService, TSource, TTarget, TOptions>(options);

            var transformBlock = new TransformBlock<TSource, TTarget>(transformer, _context.LoggerFactory, 1);
            _context.Definition.Blocks.Enqueue(transformBlock, _context.Definition.Blocks.Count + 100);

            return new PipelineTransformer<TSource, TTarget>(_context);
        }

        public IPipelineTransformer<TSource, TTarget> AddTransformer<TService, TTarget, TOptions>(Action<TOptions>? configure = null)
            where TService : notnull, ITransformer<TSource, TTarget, TOptions>
        {
            var options = Activator.CreateInstance<TOptions>();
            configure?.Invoke(options);

            return AddTransformer<TService, TTarget, TOptions>(options);
        }

        public IPipelineTransformer<TSource, TTarget> Transform<TTarget>(Func<TSource[], Mapper<TSource, TTarget>, Task<TTarget[]>> transform, Action<MapperConfiguration<TSource, TTarget>>? configure = null)
        {
            var mapperConfiguration = new MapperConfiguration<TSource, TTarget>();
            configure?.Invoke(mapperConfiguration);

            var options = new DefaultTransformerOptions<TSource, TTarget>();
            options.Transform = transform;
            options.Mapper = new Mapper<TSource, TTarget>(mapperConfiguration);

            return AddTransformer<DefaultTransformer<TSource, TTarget>, TTarget, DefaultTransformerOptions<TSource, TTarget>>(options);
        }

        public IPipelineTransformer<TSource, TTarget> Transform<TTarget>(Func<TSource[], Mapper<TSource, TTarget>, TTarget[]> transform, Action<MapperConfiguration<TSource, TTarget>>? configure = null)
        {
            var mapperConfiguration = new MapperConfiguration<TSource, TTarget>();
            configure?.Invoke(mapperConfiguration);

            var options = new DefaultSynchronousTransformerOptions<TSource, TTarget>();
            options.Transform = transform;
            options.Mapper = new Mapper<TSource, TTarget>(mapperConfiguration);

            QueueDefaultExtractor();

            var transformer = _context.ServiceFactory.CreateSynchronousTransformer<DefaultSynchronousTransformer<TSource, TTarget>, TSource, TTarget, DefaultSynchronousTransformerOptions<TSource, TTarget>>(options);

            var transformBlock = new TransformBlock<TSource, TTarget>(transformer, _context.LoggerFactory, 1);
            _context.Definition.Blocks.Enqueue(transformBlock, _context.Definition.Blocks.Count + 100);

            return new PipelineTransformer<TSource, TTarget>(_context);
        }

        public IPipelineLoader<TTarget> Load<TService, TTarget, TOptions>(TOptions options)
            where TService : notnull, IWriteConnector<TTarget, TOptions>
        {
            QueueDefaultExtractor();

            var writeConnectorFactory = () => (IWriteConnector<TTarget>)_context.ServiceFactory.CreateWriteConnector<TService, TTarget, TOptions>(options);

            return new PipelineLoader<TTarget>(_context, writeConnectorFactory);
        }

        private void QueueDefaultExtractor()
        {
            var options = new DefaultExtractorOptions<TSource>();
            var extractor = _context.ServiceFactory.CreateExtractor<DefaultExtractor<TSource>, TSource, DefaultExtractorOptions<TSource>>(options, _readConnectorFactory());

            var sourceBlock = new ExtractBlock<TSource>(extractor, _context.LoggerFactory);
            _context.Definition.Blocks.Enqueue(sourceBlock, _context.Definition.Blocks.Count + 100);
        }

        public Func<IReadConnector<TSource>> ReadConnectorFactory => _readConnectorFactory;

        public void AddPipelineBlock(Func<Task> prePipelineTaskFactory, int priority)
            => _context.Definition.PrePostPipelineBlocks.Add(new PrePostPipelineBlock(prePipelineTaskFactory, priority));
    }
}
