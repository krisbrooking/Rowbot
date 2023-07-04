using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rowbot.Transformers.Default;

namespace Rowbot.DependencyInjection
{
    public static partial class RowbotInstallerExtensions
    {
        internal static IServiceCollection AddDefaultTransformers(this IServiceCollection services)
        {
            services.TryAddTransient(typeof(DefaultTransformer<,>));
            services.TryAddTransient(typeof(DefaultSynchronousTransformer<,>));

            return services;
        }
    }
}
