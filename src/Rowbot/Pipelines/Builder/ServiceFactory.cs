namespace Rowbot.Pipelines.Builder;

public sealed class ServiceFactory(GetRequiredService serviceFactory)
{
    public TConnector CreateReadConnector<TConnector, TInput, TOutput>()
        where TConnector : IReadConnector<TInput, TOutput>
    {
        return serviceFactory.Invoke<TConnector>();
    }

    public TConnector CreateWriteConnector<TConnector, TOutput>()
        where TConnector : IWriteConnector<TOutput>
    {
        return serviceFactory.Invoke<TConnector>();
    }

    public TExtractor CreateExtractor<TExtractor, TInput, TOutput>(IReadConnector<TInput, TOutput> connector)
        where TExtractor : IExtractor<TInput, TOutput>
    {
        var service = serviceFactory.Invoke<TExtractor>();
        service.Connector = connector;

        return service;
    }

    public TTransformer CreateTransformer<TTransformer, TInput, TOutput>()
        where TTransformer : ITransformer<TInput, TOutput>
    {
        var service = serviceFactory.Invoke<TTransformer>();

        return service;
    }

    public TTransformer CreateAsyncTransformer<TTransformer, TInput, TOutput>()
        where TTransformer : IAsyncTransformer<TInput, TOutput>
    {
        var service = serviceFactory.Invoke<TTransformer>();

        return service;
    }

    public TLoader CreateLoader<TLoader, TInput>(IWriteConnector<TInput> connector)
        where TLoader : ILoader<TInput>
    {
        var service = serviceFactory.Invoke<TLoader>();
        service.Connector = connector;

        return service;
    }
}

public delegate object GetRequiredService(Type serviceType);

internal static class ServiceFactoryExtensions
{
    public static T Invoke<T>(this GetRequiredService getRequiredService) => (T)getRequiredService(typeof(T));
}