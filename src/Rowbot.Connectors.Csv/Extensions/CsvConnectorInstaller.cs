using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Rowbot.Connectors.Csv
{
    public static class CsvInstallerExtensions
    {
        public static IServiceCollection AddCsvConnector(this IServiceCollection services)
        {
            services.TryAddTransient(typeof(CsvReadConnector<,>));
            services.TryAddTransient(typeof(CsvWriteConnector<>));

            return services;
        }
    }
}
