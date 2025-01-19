using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rowbot.Common.Services;
using Rowbot.Connectors.Common.Database;
using Rowbot.Connectors.Common.Find;
using Rowbot.Connectors.Common.Synchronisation;
using Rowbot.Connectors.List;
using Rowbot.Entities;
using Rowbot.Extractors.CursorPagination;
using Rowbot.Extractors;
using Rowbot.Extractors.OffsetPagination;
using Rowbot.Loaders;
using Rowbot.Loaders.SlowlyChangingDimension;
using Rowbot.Loaders.SnapshotFact;
using Rowbot.Loaders.TransactionFact;
using Rowbot.Pipelines.Builder;
using Rowbot.Pipelines.Summary;

namespace Rowbot;

public static class ServiceRegistrationExtensions
{
    /// <summary>
    /// Registers required services with dependency injection. Also scans for pipelines in the current assembly,
    /// or in a list of assemblies supplied as arguments, and registers those with the pipeline runner.
    /// </summary>
    public static IServiceCollection AddRowbot(this IServiceCollection services, params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
        {
            assemblies = [Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()];
        }

        services.RegisterServices();
        services.RegisterPipelines(assemblies);

        services.AddListConnector();

        services.AddExtractors();
        services.AddTransformers();
        services.AddLoaders();

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
            services.AddTransient(typeof(IPipeline), type);
        }

        services.RegisterServices();

        services.AddListConnector();

        services.AddExtractors();
        services.AddTransformers();
        services.AddLoaders();

        return services;
    }

    private static IServiceCollection RegisterPipelines(this IServiceCollection services, Assembly[] assemblies)
    {
        var typesImplementingPipeline = assemblies
            .SelectMany(x => x.ExportedTypes)
            .Where(x => typeof(IPipeline).IsAssignableFrom(x));

        var types = assemblies.SelectMany(x => x.ExportedTypes).ToList();

        foreach (var type in typesImplementingPipeline.Where(x => x != typeof(IPipeline)))
        {
            services.AddTransient(typeof(IPipeline), type);
        }

        return services;
    }

    private static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        services.TryAddSingleton<IPipelineRunner, PipelineRunner>();
        services.TryAddTransient<IPipelineBuilder, PipelineBuilder>();

        services.TryAddSingleton(provider => (ServiceFactory)Activator.CreateInstance(typeof(ServiceFactory), [ (GetRequiredService)provider.GetRequiredService ])!);

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
    
    private static IServiceCollection AddListConnector(this IServiceCollection services)
    {
        services.TryAddTransient(typeof(ListReadConnector<,>));
        services.TryAddTransient(typeof(ListWriteConnector<>));

        return services;
    }
    
    private static IServiceCollection AddExtractors(this IServiceCollection services)
    {
        services.TryAddTransient(typeof(DefaultExtractor<,>));
        services.TryAddTransient(typeof(InlineExtractor<,>));
        services.TryAddTransient(typeof(CursorPaginationExtractor<,,>));
        services.TryAddTransient(typeof(OffsetPaginationExtractor<,>));

        return services;
    }
    
    private static IServiceCollection AddTransformers(this IServiceCollection services)
    {
        services.TryAddTransient(typeof(Transformer<,>));
        services.TryAddTransient(typeof(AsyncTransformer<,>));
        services.TryAddTransient(typeof(MapperTransformer<,>));

        return services;
    }
    
    private static IServiceCollection AddLoaders(this IServiceCollection services)
    {
        services.TryAddTransient(typeof(DefaultLoader<>));
        services.TryAddTransient(typeof(TransactionFactLoader<>));
        services.TryAddTransient(typeof(SnapshotFactLoader<>));
        services.TryAddTransient(typeof(SlowlyChangingDimensionLoader<>));

        return services;
    }
}