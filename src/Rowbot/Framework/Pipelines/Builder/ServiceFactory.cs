using Rowbot.Framework.Pipelines.Options;

namespace Rowbot.Framework.Pipelines.Builder
{
    public sealed class ServiceFactory
    {
        private readonly GetRequiredService _getRequiredService;

        public ServiceFactory(GetRequiredService serviceFactory)
        {
            _getRequiredService = serviceFactory;
        }

        public TService CreateReadConnector<TService, TSource, TOptions>(TOptions options)
            where TService : notnull, IReadConnector<TSource, TOptions>
        {
            var service = _getRequiredService.Invoke<TService>();
            service.Options = options;

            return service;
        }

        public TService CreateWriteConnector<TService, TSource, TOptions>(TOptions options)
            where TService : notnull, IWriteConnector<TSource, TOptions>
        {
            var service = _getRequiredService.Invoke<TService>();
            service.Options = options;

            return service;
        }

        public TService CreateExtractor<TService, TSource, TOptions>(TOptions options, IReadConnector<TSource> connector)
            where TService : notnull, IExtractor<TSource, TOptions>
            where TOptions : ExtractorOptions
        {
            var service = _getRequiredService.Invoke<TService>();
            service.Options = options;
            service.Connector = connector;

            return service;
        }

        public TService CreateTransformer<TService, TSource, TTarget, TOptions>(TOptions options)
            where TService : notnull, ITransformer<TSource, TTarget, TOptions>
            where TOptions : TransformerOptions
        {
            var service = _getRequiredService.Invoke<TService>();
            service.Options = options;

            return service;
        }

        internal TService CreateSynchronousTransformer<TService, TSource, TTarget, TOptions>(TOptions options)
            where TService : notnull, ISynchronousTransformer<TSource, TTarget, TOptions>
            where TOptions : TransformerOptions
        {
            var service = _getRequiredService.Invoke<TService>();
            service.Options = options;

            return service;
        }

        public TService CreateLoader<TService, TTarget, TOptions>(TOptions options, IWriteConnector<TTarget> connector)
            where TService : notnull, ILoader<TTarget, TOptions>
            where TOptions : LoaderOptions
        {
            var service = _getRequiredService.Invoke<TService>();
            service.Options = options;
            service.Connector = connector;

            return service;
        }
    }

    public delegate object GetRequiredService(Type serviceType);

    internal static class ServiceFactoryExtensions
    {
        public static T Invoke<T>(this GetRequiredService getRequiredService) => (T)getRequiredService(typeof(T));
    }
}
