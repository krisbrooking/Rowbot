namespace Rowbot.Connectors.Http;

public static class HttpConnectorExtensions
{
    public static IExtractBuilderConnectorStep<TInput, TOutput> FromJsonEndpoint<TInput, TOutput>(
        this IExtractBuilder<TInput, TOutput> builder,
        string requestUri)
    {
        return builder.WithConnector<JsonEndpointReadConnector<TInput, TOutput>>(connector =>
        {
            connector.RequestUri = requestUri;
        });
    }
    
    public static IExtractBuilderConnectorStep<TInput, TOutput> FromJsonEndpoint<TInput, TOutput>(
        this IExtractBuilder<TInput, TOutput> builder,
        string requestUri,
        Action<HttpClient> configureHttpClient)
    {
        return builder.WithConnector<JsonEndpointReadConnector<TInput, TOutput>>(connector =>
        {
            connector.RequestUri = requestUri;
            connector.HttpClientConfigurator = (httpClient, _) => configureHttpClient.Invoke(httpClient);
        });
    }
    
    public static IExtractBuilderConnectorStep<TInput, TOutput> FromJsonEndpoint<TInput, TOutput>(
        this IExtractBuilder<TInput, TOutput> builder,
        string requestUri,
        Action<HttpClient, Uri> configureHttpClient)
    {
        return builder.WithConnector<JsonEndpointReadConnector<TInput, TOutput>>(connector =>
        {
            connector.RequestUri = requestUri;
            connector.HttpClientConfigurator = configureHttpClient;
        });
    }
}