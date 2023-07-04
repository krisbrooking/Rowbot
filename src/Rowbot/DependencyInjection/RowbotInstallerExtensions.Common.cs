using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rowbot.Common.Services;
using Rowbot.DependencyInjection;
using Rowbot.Entities;
using Rowbot.Framework.Blocks.Connectors.Database;
using Rowbot.Framework.Blocks.Connectors.Find;
using Rowbot.Framework.Blocks.Connectors.Synchronisation;
using Rowbot.Framework.Pipelines.Builder;
using Rowbot.Framework.Pipelines.Summary;
using System.Reflection;

namespace Rowbot
{
    public static partial class RowbotInstallerExtensions
    {
        /// <summary>
        /// Registers required services with dependency injection. Also scans for pipelines in the current assembly, or in a list of assemblies supplied as arguments, and registers those with the pipeline runner.
        /// </summary>
        public static IServiceCollection AddRowbot(this IServiceCollection services, params Assembly[] assemblies)
        {
            if (assemblies.Length > 0)
            {
                RowbotInstaller.Instance.Assemblies = assemblies;
            }

            services.RegisterPipelines();
            services.RegisterServices();

            services.AddListConnector();
            services.AddJsonConnector();

            services.AddDefaultExtractor();
            services.AddCursorPaginationExtractor();
            services.AddOffsetPaginationExtractor();

            services.AddDefaultTransformers();

            services.AddRowLoader();
            services.AddFactLoader();
            services.AddSnapshotFactLoader();
            services.AddSlowlyChangingDimensionLoader();

            return services;
        }

        /// <summary>
        /// <para>
        /// Registers required services with dependency injection. Accepts an array of pipeline container types to be registered with the pipeline runner. 
        /// </para>
        /// <para>
        /// This overload is intended for integration testing.
        /// </para>
        /// </summary>
        public static IServiceCollection AddRowbot(this IServiceCollection services, Type[] pipelineContainerTypes)
        {
            foreach (var type in pipelineContainerTypes)
            {
                services.AddTransient(typeof(IPipelineContainer), type);
            }

            services.RegisterServices();

            services.AddListConnector();
            services.AddJsonConnector();

            services.AddDefaultExtractor();
            services.AddCursorPaginationExtractor();
            services.AddOffsetPaginationExtractor();

            services.AddDefaultTransformers();

            services.AddRowLoader();
            services.AddFactLoader();
            services.AddSnapshotFactLoader();
            services.AddSlowlyChangingDimensionLoader();

            return services;
        }

        internal static IServiceCollection RegisterPipelines(this IServiceCollection services)
        {
            var typesImplementingPipeline = RowbotInstaller.Instance.Assemblies
                .SelectMany(x => x.ExportedTypes)
                .Where(x => typeof(IPipelineContainer).IsAssignableFrom(x));

            foreach (var type in typesImplementingPipeline.Where(x => x != typeof(IPipelineContainer)))
            {
                services.AddTransient(typeof(IPipelineContainer), type);
            }

            return services;
        }

        internal static IServiceCollection RegisterServices(this IServiceCollection services)
        {
            services.TryAddSingleton<IPipelineRunner, PipelineRunner>();
            services.TryAddTransient<IPipelineBuilder, PipelineBuilder>();

            services.TryAddSingleton(provider => (ServiceFactory)Activator.CreateInstance(typeof(ServiceFactory), new object[] { (GetRequiredService)provider.GetRequiredService })!);

            services.TryAddTransient(typeof(IEntity<>), typeof(Entity<>));
            services.TryAddTransient<IFinderProvider, FinderProvider>();
            services.TryAddSingleton<ISharedLockManager, SharedLockManager>();
            services.TryAddSingleton<ISystemClock, SystemClock>();
            services.TryAddTransient(typeof(ISqlCommandProvider<,>), typeof(SqlCommandProvider<,>));

            return services;
        }

        /// <summary>
        /// Displays a summary of pipeline execution at the end of a run. Requires .NET Console project.
        /// </summary>
        public static IServiceCollection AddConsoleSummary(this IServiceCollection services)
        {
            services.AddTransient<ISummaryOutput, ConsoleSummaryOutput>();

            return services;
        }
    }
}
