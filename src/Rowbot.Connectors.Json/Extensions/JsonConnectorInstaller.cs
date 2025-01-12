using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Rowbot.Connectors.Json;

public static class JsonConnectorInstaller
{
    public static IServiceCollection AddJsonConnector(this IServiceCollection services)
    {
        services.TryAddTransient(typeof(JsonReadConnector<,>));

        return services;
    }
}