using Rowbot.Framework.Blocks;
using Rowbot.Framework.Pipelines;
using Rowbot.Framework.Pipelines.Builder;
using Rowbot.Framework.Pipelines.Options;

namespace Rowbot
{
    public interface ICustomPipelineLoader<TTarget> : ICustomPipeline
    {
        Func<IWriteConnector<TTarget>> WriteConnectorFactory { get; }
    }

    public interface IPipelineLoader<TTarget>
    {
        /// <summary>
        /// Adds a loader. <typeparamref name="TService"/> must be registered for dependency injection.
        /// </summary>
        /// <typeparam name="TService">A loader that implements <see cref="ILoader{TTarget, TOptions}"/></typeparam>
        /// <typeparam name="TOptions">The options type of the loader</typeparam>
        Pipeline WithLoader<TService, TOptions>(TOptions options)
            where TService : notnull, ILoader<TTarget, TOptions>
            where TOptions : LoaderOptions;
    }

    public sealed class PipelineLoader<TTarget> : IPipelineLoader<TTarget>, ICustomPipelineLoader<TTarget>
    {
        internal readonly PipelineBuilderContext _context;
        private readonly Func<IWriteConnector<TTarget>> _writeConnectorFactory;

        public PipelineLoader(PipelineBuilderContext context, Func<IWriteConnector<TTarget>> writeConnectorFactory)
        {
            _context = context;
            _writeConnectorFactory = writeConnectorFactory;
        }

        public Pipeline WithLoader<TService, TOptions>(TOptions options)
            where TService : notnull, ILoader<TTarget, TOptions>
            where TOptions : LoaderOptions
        {
            _context.Definition.DependencyResolution.SetTargetEntity(typeof(TService));

            var loader = _context.ServiceFactory.CreateLoader<TService, TTarget, TOptions>(options, _writeConnectorFactory());

            var loaderBlock = new LoadBlock<TTarget>(loader, _context.LoggerFactory, 1);

            _context.Definition.Blocks.Enqueue(loaderBlock, _context.Definition.Blocks.Count + 100);

            _context.Definition.BlockContext.LoaderOptions = options;

            var writeConnector = _writeConnectorFactory();
            if (writeConnector is ISchemaConnector schemaConnector)
            {
                AddPipelineBlock(async () =>
                {
                    await schemaConnector.CreateDataSetAsync();
                },
                50);
            }

            _context.Definition.IsDirty = true;

            return new Pipeline(_context.Definition);
        }

        public Func<IWriteConnector<TTarget>> WriteConnectorFactory => _writeConnectorFactory;

        public void AddPipelineBlock(Func<Task> prePipelineTaskFactory, int priority)
            => _context.Definition.PrePostPipelineBlocks.Add(new PrePostPipelineBlock(prePipelineTaskFactory, priority));
    }
}
