using Rowbot.Pipelines.Blocks;
using Rowbot.Pipelines.Builder;
using Rowbot.Pipelines.Summary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Rowbot.Common.Extensions;
using Rowbot.Pipelines.Runner.DependencyResolution;
using Rowbot.Pipelines.Tasks;
using Microsoft.Extensions.Logging;

namespace Rowbot;

public sealed class Pipeline
{
    private readonly PipelineBuilderContext _context;

    internal Pipeline(PipelineBuilderContext context)
    {
        _context = context;
    }

    internal DependencyResolutionMetadata Metadata => _context.DependencyResolution;
    public string? Name { get; set; }
    public string? Container { get; set; }
    public string? Cluster { get; set; }

    public async Task<PipelineSummary> InvokeAsync()
    {
        var logger = _context.LoggerFactory.CreateLogger<Pipeline>();

        var blockSummaries = new ConcurrentBag<BlockSummary>();
        Action<BlockSummary> summaryCallback = (summary) => blockSummaries.Add(summary);

        foreach (var block in _context.Blocks)
        {
            block.SummaryCallback = summaryCallback;
        }

        var pipelineTasks = LinkBlocks(_context.Blocks);

        var watch = new Stopwatch();
        watch.Start();

        var prePipelineTaskSummaries = new List<BlockSummary>();
        var postPipelineTaskSummaries = new List<BlockSummary>();
        try
        {
            using var cts = new CancellationTokenSource();

            prePipelineTaskSummaries = await ExecuteTasksAsync(_context.PrePipelineTasks).ConfigureAwait(false);

            await Task.WhenAll(pipelineTasks.Select(x => x(cts))).ConfigureAwait(false);

            postPipelineTaskSummaries = await ExecuteTasksAsync(_context.PostPipelineTasks).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pipeline {Container}:{Name} execution failed", Container ?? "N/A", Name ?? "N/A");
        }


        watch.Stop();

        List<BlockSummary> summaries = [.. prePipelineTaskSummaries, .. blockSummaries.ToList(), ..postPipelineTaskSummaries];
        return new PipelineSummary(Cluster ?? "N/A", Container ?? "N/A", Name ?? "N/A", summaries, watch.Elapsed);
    }

    private async Task<List<BlockSummary>> ExecuteTasksAsync(PriorityQueue<ITask, int> taskQueue)
    {
        var summaries = new List<BlockSummary>();
        
        while (taskQueue.Count > 0)
        {
            var task = taskQueue.Dequeue();
            summaries.Add(task.IsAsync ? await task.RunAsync() : task.Run());
        }
        
        return summaries;
    }
    
    internal static IReadOnlyList<Func<CancellationTokenSource, Task>> LinkBlocks(Queue<IBlock> blockQueue)
    {
        if (blockQueue.Count < 2)
        {
            throw new BlockBuilderException($"Too few blocks");
        }

        List<Func<CancellationTokenSource, Task>> pipelineTasks = [];

        var queueCount = blockQueue.Count;

        var previousBlock = blockQueue.Dequeue();

        for (var index = 0; index < queueCount - 1; index++)
        {
            var nextBlock = blockQueue.Dequeue();
            
            if (previousBlock.GetType().ImplementsGenericInterface(typeof(IBlockSource<>)) &&
                nextBlock.GetType().ImplementsGenericInterface(typeof(IBlockTarget<>)))
            {
                try
                {
                    GetLinkToMethod(previousBlock, index + 1)
                        .Invoke(previousBlock, [ nextBlock ]);

                    pipelineTasks.Add(previousBlock.PrepareTask());
                }
                catch (BlockBuilderException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new BlockBuilderException($"Cannot link previous block of type {previousBlock.GetType()} to next block of type {nextBlock.GetType()}", ex);
                }
            }
            else
            {
                pipelineTasks.Add(previousBlock.PrepareTask());
            }
            
            previousBlock = nextBlock;
        }
        
        pipelineTasks.Add(previousBlock.PrepareTask());

        return pipelineTasks;
    }

    private static MethodInfo GetLinkToMethod(IBlock block, int index)
    {
        var blockType = block
            .GetType()
            .GetInterfaces()
            .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IBlockSource<>));
        if (blockType is null)
        {
            throw new BlockBuilderException($"Block at index {index} does not implement IBlockSource<>");
        }

        var linkToMethod = typeof(IBlockSource<>)
            .MakeGenericType(blockType.GetGenericArguments().Last())
            .GetMethod("LinkTo");
        if (linkToMethod is null)
        {
            throw new BlockBuilderException($"Block at index {index} does not implement LinkTo method");
        }

        return linkToMethod;
    }
}
