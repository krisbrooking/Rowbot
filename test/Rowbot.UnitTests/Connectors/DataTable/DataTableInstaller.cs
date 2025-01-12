using Microsoft.Extensions.DependencyInjection;

namespace Rowbot.UnitTests.Connectors.DataTable
{
    public static class DataTableInstaller
    {
        public static IServiceCollection AddDataTableConnector(this IServiceCollection services)
        {
            if (!services.Any(x => x.ServiceType == typeof(DataTableReadConnector<,>)))
            {
                services.AddTransient(typeof(DataTableReadConnector<,>), typeof(DataTableReadConnector<,>));
            }

            if (!services.Any(x => x.ServiceType == typeof(DataTableWriteConnector<>)))
            {
                services.AddTransient(typeof(DataTableWriteConnector<>), typeof(DataTableWriteConnector<>));
            }

            return services;
        }
    }
}
