using Rowbot.Framework.Blocks;
using Rowbot.Framework.Pipelines.Builder;
using Rowbot.Framework.Pipelines.Summary;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Rowbot
{
    public sealed class Pipeline
    {
        private readonly PipelineDefinition _definition;

        internal Pipeline(PipelineDefinition definition)
        {
            _definition = definition;
        }

        internal DependencyResolutionMetadata Metadata => _definition.DependencyResolution;
        public string? Name { get; set; }
        public string? Container { get; set; }
        public string? Cluster { get; set; }

        public async Task<PipelineSummary> InvokeAsync()
        {
            var linker = new BlockLinker();

            var blockSummaries = new ConcurrentBag<IBlockSummary>();
            _definition.BlockContext.SummaryCallback = (summary) => blockSummaries.Add(summary);

            foreach (var block in _definition.PrePostPipelineBlocks)
            {
                var taskBlock = new TaskBlock(block.TaskFactory);
                _definition.Blocks.Enqueue(taskBlock, block.Priority);
            }

            var pipelineTaskGroups = linker.LinkBlocks(_definition.Blocks, _definition.BlockContext);

            var watch = new Stopwatch();
            watch.Start();

            foreach (var group in pipelineTaskGroups)
            {
                var tasks = group.Select(x => x());

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            watch.Stop();

            return new PipelineSummary(Cluster ?? "N/A", Container ?? "N/A", Name ?? "N/A", blockSummaries.ToList(), watch.Elapsed);
        }
    }
}
