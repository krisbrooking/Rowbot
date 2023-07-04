using Rowbot.Framework.Blocks;
using Rowbot.Framework.Pipelines.Builder;
using Rowbot.Transformers.Default;

namespace Rowbot
{
    /// <summary>
    /// Generic IPipelineTransformer creates a chain of transform blocks.
    /// </summary>
    /// <typeparam name="TPrevious">Source type of previous block</typeparam>
    /// <typeparam name="TSource">Source type of current block</typeparam>
    /// <remarks>
    /// IPipelineTransformer takes two generic arguments which represent the source and target types of the previous block in the chain.
    /// TPrevious is the source type of the previous block. It is unused by the current block but necessary to create a chain of IPipelineTransformers.
    /// TSource is the target type of the previous block, and therefore, the source type of the current block.
    /// Transformation from source type to target type is achieved with a generic method argument, TTarget. To continue the chain, this generic 
    /// argument is returned as the TSource type of the next block in the chain.
    /// </remarks>
    public interface IPipelineTransformer<TPrevious, TSource>
    {
        /// <summary>
        /// Adds a transformer. <typeparamref name="TService"/> must be registered for dependency injection.
        /// </summary>
        /// <typeparam name="TService">A transformer that implements <see cref="ITransformer{TSource, TTarget, TOptions}"/></typeparam>
        /// <typeparam name="TTarget">The type that this transformer, transforms data into</typeparam>
        /// <typeparam name="TOptions">The options type of the transformer</typeparam>
        IPipelineTransformer<TSource, TTarget> AddTransformer<TService, TTarget, TOptions>(TOptions options)
            where TService : notnull, ITransformer<TSource, TTarget, TOptions>;

        /// <inheritdoc cref="AddTransformer{TService, TTarget, TOptions}(TOptions)"/>
        IPipelineTransformer<TSource, TTarget> AddTransformer<TService, TTarget, TOptions>(Action<TOptions>? configure = null)
            where TService : notnull, ITransformer<TSource, TTarget, TOptions>;

        /// <summary>
        /// Transforms data with the default asynchronous transformer.
        /// </summary>
        /// <typeparam name="TTarget"></typeparam>
        /// <param name="transform">An async lambda function to perform transformation</param>
        /// <param name="configure">Optionally include a mapper configuration</param>

        // Ideally, this would be an extension method. However, the transformer chaining described above does not work as
        // well with extension methods because type inference breaks when calling AddTransformer<TService, TTarget, TOptions>.
        // Note: custom transformers can be used with transformer chaining but without type inference of TSource.
        IPipelineTransformer<TSource, TTarget> Transform<TTarget>(Func<TSource[], Mapper<TSource, TTarget>, Task<TTarget[]>> transform, Action<MapperConfiguration<TSource, TTarget>>? configure = null);

        /// <summary>
        /// Transforms data with the default transformer.
        /// </summary>
        /// <typeparam name="TTarget"></typeparam>
        /// <param name="transform">A lambda function to perform transformation</param>
        /// <param name="configure">Optionally include a mapper configuration</param>
        IPipelineTransformer<TSource, TTarget> Transform<TTarget>(Func<TSource[], Mapper<TSource, TTarget>, TTarget[]> transform, Action<MapperConfiguration<TSource, TTarget>>? configure = null);

        /// <summary>
        /// Adds a write connector. <typeparamref name="TService"/> must be registered for dependency injection.
        /// </summary>
        /// <typeparam name="TService">A write connector that implements <see cref="IWriteConnector{TTarget, TOptions}"/></typeparam>
        /// <typeparam name="TTarget">The data type to load</typeparam>
        /// <typeparam name="TOptions">The options type of the write connector</typeparam>
        IPipelineLoader<TTarget> Load<TService, TTarget, TOptions>(TOptions options)
            where TService : notnull, IWriteConnector<TTarget, TOptions>;
    }

    public sealed class PipelineTransformer<TPrevious, TSource> : IPipelineTransformer<TPrevious, TSource>
    {
        internal readonly PipelineBuilderContext _context;

        public PipelineTransformer(PipelineBuilderContext context)
        {
            _context = context;
        }

        public IPipelineTransformer<TSource, TTarget> AddTransformer<TService, TTarget, TOptions>(TOptions options)
            where TService : notnull, ITransformer<TSource, TTarget, TOptions>
        {
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

            var transformer = _context.ServiceFactory.CreateSynchronousTransformer<DefaultSynchronousTransformer<TSource, TTarget>, TSource, TTarget, DefaultSynchronousTransformerOptions<TSource, TTarget>>(options);

            var transformBlock = new TransformBlock<TSource, TTarget>(transformer, _context.LoggerFactory, 1);
            _context.Definition.Blocks.Enqueue(transformBlock, _context.Definition.Blocks.Count + 100);

            return new PipelineTransformer<TSource, TTarget>(_context);
        }

        public IPipelineLoader<TTarget> Load<TService, TTarget, TOptions>(TOptions options)
            where TService : notnull, IWriteConnector<TTarget, TOptions>
        {
            var writeConnectorFactory = () => (IWriteConnector<TTarget>)_context.ServiceFactory.CreateWriteConnector<TService, TTarget, TOptions>(options);

            return new PipelineLoader<TTarget>(_context, writeConnectorFactory);
        }
    }
}
