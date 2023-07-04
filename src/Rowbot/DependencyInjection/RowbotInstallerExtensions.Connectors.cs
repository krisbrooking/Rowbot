using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rowbot.Connectors.Json;
using Rowbot.Connectors.List;

namespace Rowbot
{
    public static partial class RowbotInstallerExtensions
    {
        internal static IServiceCollection AddListConnector(this IServiceCollection services)
        {
            services.TryAddTransient(typeof(ListReadConnector<>));

            return services;
        }

        internal static IServiceCollection AddJsonConnector(this IServiceCollection services)
        {
            services.TryAddTransient(typeof(JsonReadConnector<>));

            return services;
        }
    }
}
