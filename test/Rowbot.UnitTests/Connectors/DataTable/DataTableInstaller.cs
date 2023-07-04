using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Rowbot.UnitTests.Connectors.DataTable
{
    public static class DataTableInstaller
    {
        public static IServiceCollection AddDataTableConnector(this IServiceCollection services)
        {
            if (!services.Any(x => x.ServiceType == typeof(IDataTableReadConnector<>)))
            {
                services.AddTransient(typeof(IDataTableReadConnector<>), typeof(DataTableReadConnector<>));
            }

            if (!services.Any(x => x.ServiceType == typeof(IDataTableWriteConnector<>)))
            {
                services.AddTransient(typeof(IDataTableWriteConnector<>), typeof(DataTableWriteConnector<>));
            }

            return services;
        }
    }
}
