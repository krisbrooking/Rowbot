using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Rowbot.Connectors.Sqlite
{
    public static class SqliteInstallerExtensions
    {
        public static IServiceCollection AddSqliteConnector(this IServiceCollection services)
        {
            services.TryAddTransient(typeof(SqliteReadConnector<,>));
            services.TryAddTransient(typeof(SqliteWriteConnector<>));
            services.TryAddTransient(typeof(SqliteCommandProvider));

            return services;
        }
    }
}
