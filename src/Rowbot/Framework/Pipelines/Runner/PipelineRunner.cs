using Microsoft.Extensions.Logging;
using Rowbot.Framework.Pipelines.Runner;
using Rowbot.Framework.Pipelines.Runner.DependencyResolution;
using Rowbot.Framework.Pipelines.Summary;
using System.Reflection;

namespace Rowbot
{
    public interface IPipelineRunner
    {
        /// <summary>
        /// Run all pipelines.
        /// </summary>
        /// <returns>A summary of every pipeline executed</returns>
        Task<IEnumerable<PipelineSummary>> RunAsync();
        /// <summary>
        /// Run a subset of all pipelines by filtering on cluster and tag.
        /// </summary>
        /// <param name="configure">Configure pipeline runner by filtering by cluster or tag or both. Cluster filter is set to <see cref="PipelineCluster.Default"/> by default.</param>
        /// <returns>A summary of every pipeline executed</returns>
        Task<IEnumerable<PipelineSummary>> RunAsync(Action<PipelineRunnerOptions> configure);
        /// <summary>
        /// Run all pipelines in a single pipeline container.
        /// </summary>
        /// <typeparam name="TPipelineContainer">Pipeline container class type</typeparam>
        /// <returns>A summary of every pipeline executed</returns>
        Task<IEnumerable<PipelineSummary>> RunAsync<TPipelineContainer>() where TPipelineContainer : IPipelineContainer;
        /// <summary>
        /// Run a subset of all pipelines in a single pipeline container by filtering on cluster and tag.
        /// </summary>
        /// <typeparam name="TPipelineContainer">Pipeline container class type</typeparam>
        /// <param name="configure">Configure pipeline runner by filtering by cluster or tag or both. Cluster filter is set to <see cref="PipelineCluster.Default"/> by default.</param>
        /// <returns>A summary of every pipeline executed</returns>
        Task<IEnumerable<PipelineSummary>> RunAsync<TPipelineContainer>(Action<PipelineRunnerOptions> configure) where TPipelineContainer : IPipelineContainer;
    }

    public sealed class PipelineRunner : IPipelineRunner
    {
        private readonly ILogger<PipelineRunner> _logger;
        private readonly IEnumerable<IPipelineContainer> _pipelineContainers;
        private readonly IEnumerable<ISummaryOutput> _summaryOutputs;

        public PipelineRunner(
            ILogger<PipelineRunner> logger, 
            IEnumerable<IPipelineContainer> pipelineContainers,
            IEnumerable<ISummaryOutput> summaryOutputs)
        {
            _logger = logger;
            _pipelineContainers = pipelineContainers;
            _summaryOutputs = summaryOutputs;
        }

        public async Task<IEnumerable<PipelineSummary>> RunAsync() => await ExecutePipelinesAsync(x => true, x => true);

        public async Task<IEnumerable<PipelineSummary>> RunAsync(Action<PipelineRunnerOptions> configure)
        {
            var options = new PipelineRunnerOptions();
            configure.Invoke(options);

            Func<IPipelineContainer, bool> containerPredicate = x =>
                options.Clusters.Length == 0 ||
                (
                    x.GetType().GetCustomAttribute<ClusterAttribute>() is null &&
                    options.Clusters.Any(x => x == nameof(PipelineCluster.Default))
                ) ||
                (
                    x.GetType().GetCustomAttribute<ClusterAttribute>() is not null &&
                    options.Clusters.Any(cluster => x.GetType().GetCustomAttribute<ClusterAttribute>()?.Name == cluster)
                );

            Func<MethodInfo, bool> methodsPredicate = x =>
                options.Tags.Length == 0 ||
                (
                    x.GetCustomAttribute<TagAttribute>() is not null &&
                    options.Tags.Any(tag => x.GetCustomAttribute<TagAttribute>()?.Tags.Contains(tag) ?? false)
                );

            return await ExecutePipelinesAsync(containerPredicate, methodsPredicate);
        }

        public async Task<IEnumerable<PipelineSummary>> RunAsync<TPipelineContainer>() where TPipelineContainer : IPipelineContainer
        {
            return await ExecutePipelinesAsync(x => x.GetType().DeclaringType == typeof(TPipelineContainer), x => x.DeclaringType == typeof(TPipelineContainer));
        }

        public async Task<IEnumerable<PipelineSummary>> RunAsync<TPipelineContainer>(Action<PipelineRunnerOptions> configure) where TPipelineContainer : IPipelineContainer
        {
            var options = new PipelineRunnerOptions();
            configure.Invoke(options);

            Func<IPipelineContainer, bool> containerPredicate = x =>
                x.GetType().DeclaringType == typeof(TPipelineContainer) &&
                (
                    options.Clusters.Length == 0 ||
                    (
                        x.GetType().GetCustomAttribute<ClusterAttribute>() is null &&
                        options.Clusters.Any(x => x == nameof(PipelineCluster.Default))
                    ) ||
                    (
                        x.GetType().GetCustomAttribute<ClusterAttribute>() is not null &&
                        options.Clusters.Any(cluster => x.GetType().GetCustomAttribute<ClusterAttribute>()?.Name == cluster)
                    )
                );

            Func<MethodInfo, bool> methodsPredicate = x =>
                x.DeclaringType == typeof(TPipelineContainer) &&
                (
                    options.Tags.Length == 0 ||
                    (
                        x.GetCustomAttribute<TagAttribute>() is not null &&
                        options.Tags.Any(tag => x.GetCustomAttribute<TagAttribute>()?.Tags.Contains(tag) ?? false)
                    )
                );

            return await ExecutePipelinesAsync(containerPredicate, methodsPredicate);
        }

        private async Task<IEnumerable<PipelineSummary>> ExecutePipelinesAsync(Func<IPipelineContainer, bool> containerPredicate, Func<MethodInfo, bool> methodsPredicate)
        {
            var pipelineGroups = GroupPipelinesByDependency(containerPredicate, methodsPredicate);

            var dependencyResolver = new EntityDependencyResolver();
            var pipelineSummaries = new List<PipelineSummary>();

            Task<IEnumerable<PipelineSummary>[]>? aggregationTask = null;
            try
            {
                var tasks = pipelineGroups
                    .Select(x => dependencyResolver.Resolve(x.Value))
                    .Where(x => x.Any())
                    .Select(async x => await ExecutePipelineGroupAsync(x));

                aggregationTask = Task.WhenAll(tasks);
                var result = await aggregationTask;

                pipelineSummaries = result.SelectMany(x => x).ToList();
            }
            catch
            {
                if (aggregationTask?.Exception?.InnerExceptions != null && 
                    aggregationTask.Exception.InnerExceptions.Any())
                {
                    foreach (var innerEx in aggregationTask.Exception.InnerExceptions)
                    {
                        _logger.LogError(innerEx, innerEx.Message);
                    }
                }
            }

            foreach (var summaryOutput in _summaryOutputs)
            {
                await summaryOutput.OutputAsync(pipelineSummaries);
            }

            return pipelineSummaries;
        }

        private Dictionary<PipelineCluster, List<Pipeline>> GroupPipelinesByDependency(Func<IPipelineContainer, bool> containerPredicate, Func<MethodInfo, bool> methodsPredicate)
        {
            var pipelineGroups = new Dictionary<PipelineCluster, List<Pipeline>>()
            {
                { PipelineCluster.Default, new List<Pipeline>() }
            };

            foreach (IPipelineContainer pipelineContainer in _pipelineContainers.Where(containerPredicate))
            {
                var clusterAttribute = pipelineContainer.GetType().GetCustomAttribute<ClusterAttribute>();
                var cluster = clusterAttribute is { }
                    ? new PipelineCluster(clusterAttribute)
                    : PipelineCluster.Default;

                if (!pipelineGroups.ContainsKey(cluster))
                {
                    pipelineGroups.TryAdd(cluster, new List<Pipeline>());
                }

                var methods = pipelineContainer.GetType()
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(methodsPredicate)
                    .Where(x => x.ReturnType == typeof(Pipeline) && x.GetParameters().Length == 0)
                    .ToArray();

                if (methods.Length == 0)
                {
                    continue;
                }

                for (var index = 0; index < methods.Length; index++)
                {
                    if (methods[index].Invoke(pipelineContainer, new object[0] { }) is Pipeline pipeline)
                    {
                        pipeline.Cluster = cluster.Name;
                        pipeline.Container = methods[index].DeclaringType?.Name ?? "N/A";
                        pipeline.Name = methods[index].Name;
                        pipelineGroups[cluster].Add(pipeline);
                    }
                }
            }

            return pipelineGroups;
        }

        private async Task<IEnumerable<PipelineSummary>> ExecutePipelineGroupAsync(IEnumerable<IEnumerable<Pipeline>> pipelineGroups)
        {
            var pipelineSummaries = new List<PipelineSummary>();
            var groupCount = 1;

            foreach (var pipelineGroup in pipelineGroups)
            {
                foreach (var pipeline in pipelineGroup)
                {
                    _logger.LogInformation("Running pipeline: {container} {pipeline}", pipeline.Container, pipeline.Name);

                    var pipelineSummary = await pipeline.InvokeAsync();
                    pipelineSummary.Group = groupCount;
                    pipelineSummaries.Add(pipelineSummary);
                }
                groupCount++;
            }

            return pipelineSummaries;
        }
    }
}
