using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Rowbot.Connectors.SqlServer
{
    public static class SqlServerInstallerExtensions
    {
        public static IServiceCollection AddSqlServerConnector(this IServiceCollection services)
        {
            services.TryAddTransient(typeof(SqlServerReadConnector<>));
            services.TryAddTransient(typeof(SqlServerWriteConnector<>));
            services.TryAddTransient(typeof(SqlServerCommandProvider));

            return services;
        }
    }
}
