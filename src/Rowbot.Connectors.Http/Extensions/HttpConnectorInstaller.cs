using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;

namespace Rowbot.Connectors.Http;

public static class HttpConnectorInstaller
{
    public static IServiceCollection AddHttpConnector(
        this IServiceCollection services,
        IAsyncPolicy<HttpResponseMessage>? retryPolicy = null)
    {
        services.TryAddTransient(typeof(JsonEndpointReadConnector<,>));

        services.AddHttpClient();

        services.ConfigureAll<HttpClientFactoryOptions>(options =>
        {
            options.HttpMessageHandlerBuilderActions.Add(builder =>
            {
                var handler = new PolicyHttpMessageHandler(retryPolicy ?? GetDefaultRetryPolicy());
                builder.AdditionalHandlers.Add(handler);
            });
        });

        return services;
    }
    
    private static IAsyncPolicy<HttpResponseMessage> GetDefaultRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2,
                retryAttempt)));
    }
}