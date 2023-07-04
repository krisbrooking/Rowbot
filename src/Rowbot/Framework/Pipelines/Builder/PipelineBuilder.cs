using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rowbot.Framework.Pipelines.Builder;

namespace Rowbot
{
    public interface IPipelineBuilder
    {
        /// <summary>
        /// Adds a dependency. The pipeline runner ensures that any other pipeline that loads data of type <typeparamref name="TDependsOn"/> is executed prior to the execution of this pipeline.
        /// </summary>
        /// <typeparam name="TDependsOn">The data type on which this pipeline depends</typeparam>
        IPipelineBuilder DependsOn<TDependsOn>()
            where TDependsOn : class;

        /// <summary>
        /// Adds a read connector. <typeparamref name="TService"/> must be registered for dependency injection.
        /// </summary>
        /// <typeparam name="TService">A read connector that implements <see cref="IReadConnector{TSource, TOptions}"/></typeparam>
        /// <typeparam name="TSource">The data type to extract</typeparam>
        /// <typeparam name="TOptions">The options type of the read connector</typeparam>
        IPipelineExtractor<TSource> Extract<TService, TSource, TOptions>(TOptions options)
            where TService : notnull, IReadConnector<TSource, TOptions>;
    }

    public class PipelineBuilder : IPipelineBuilder
    {
        private readonly PipelineBuilderContext _context;

        public PipelineBuilder(ILoggerFactory loggerFactory, ServiceFactory genericServiceFactory)
        {
            _context = new(loggerFactory, genericServiceFactory);
        }

        public IPipelineBuilder DependsOn<TDependsOn>()
            where TDependsOn : class
        {
            if (_context.Definition.IsDirty)
            {
                _context.ResetDefinition();
            }

            _context.Definition.DependencyResolution.AddSourceEntity(typeof(TDependsOn));

            return this;
        }

        public IPipelineExtractor<TSource> Extract<TService, TSource, TOptions>(TOptions options)
            where TService : notnull, IReadConnector<TSource, TOptions>
        {
            if (_context.Definition.IsDirty)
            {
                _context.ResetDefinition();
            }

            _context.Definition.DependencyResolution.AddSourceEntity(typeof(TService));

            var readConnectorFactory = () => (IReadConnector<TSource>)_context.ServiceFactory.CreateReadConnector<TService, TSource, TOptions>(options);

            return new PipelineExtractor<TSource>(_context, readConnectorFactory);
        }

        public static PipelineBuilder NullInstance = new PipelineBuilder(NullLoggerFactory.Instance, new ServiceFactory((service) => null!));
    }
}
