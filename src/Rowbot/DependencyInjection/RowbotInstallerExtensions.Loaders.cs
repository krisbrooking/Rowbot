using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Rowbot
{
    public static partial class RowbotInstallerExtensions
    {
        internal static IServiceCollection AddFactLoader(this IServiceCollection services)
        {
            services.TryAddTransient(typeof(FactLoader<>));

            return services;
        }

        internal static IServiceCollection AddRowLoader(this IServiceCollection services)
        {
            services.TryAddTransient(typeof(RowLoader<>));

            return services;
        }

        internal static IServiceCollection AddSnapshotFactLoader(this IServiceCollection services)
        {
            services.TryAddTransient(typeof(SnapshotFactLoader<>));

            return services;
        }

        internal static IServiceCollection AddSlowlyChangingDimensionLoader(this IServiceCollection services)
        {
            services.TryAddTransient(typeof(SlowlyChangingDimensionLoader<>));

            return services;
        }
    }
}
