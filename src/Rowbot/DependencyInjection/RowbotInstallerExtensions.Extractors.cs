using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rowbot.Extractors.Default;

namespace Rowbot
{
    public static partial class RowbotInstallerExtensions
    {
        internal static IServiceCollection AddDefaultExtractor(this IServiceCollection services)
        {
            services.TryAddTransient(typeof(DefaultExtractor<>));

            return services;
        }

        internal static IServiceCollection AddCursorPaginationExtractor(this IServiceCollection services)
        {
            services.TryAddTransient(typeof(CursorPaginationExtractor<>));

            return services;
        }

        internal static IServiceCollection AddOffsetPaginationExtractor(this IServiceCollection services)
        {
            services.TryAddTransient(typeof(OffsetPaginationExtractor<>));

            return services;
        }
    }
}
