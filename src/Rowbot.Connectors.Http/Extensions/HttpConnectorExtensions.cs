namespace Rowbot.Connectors.Http;

public static class HttpConnectorExtensions
{
    public static IExtractBuilderConnectorStep<TInput, TOutput> FromJsonEndpoint<TInput, TOutput>(
        this IExtractBuilder<TInput, TOutput> builder, 
        Action<HttpConnectorOptions> configure)
    {
        var options = new HttpConnectorOptions();
        configure?.Invoke(options);

        return builder.WithConnector<JsonEndpointReadConnector<TInput, TOutput>>(connector => connector.Options = options);
    }
}

public class HttpConnectorOptions
{
    public string RequestUri { get; set; } = string.Empty;
    public Func<IHttpClientFactory, ExtractParameter[], HttpClient> HttpClientFactory { get; set; } = (httpClientFactory, _) => httpClientFactory.CreateClient();
}