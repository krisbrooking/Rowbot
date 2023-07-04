using Rowbot.Framework.Blocks;

namespace Rowbot.Framework.Pipelines.Builder
{
    public sealed class PipelineDefinition
    {
        public PriorityQueue<IBlock, int> Blocks { get; } = new(Comparer<int>.Create((x, y) => y - x));
        public DependencyResolutionMetadata DependencyResolution { get; set; } = new();
        public BlockContext BlockContext { get; set; } = new();
        public List<PrePostPipelineBlock> PrePostPipelineBlocks { get; } = new();
        /// <summary>
        /// True if this instance of <see cref="PipelineDefinition"/> has been used to create a pipeline.
        /// </summary>
        public bool IsDirty { get; set; }
    }

    public sealed class PrePostPipelineBlock
    {
        public PrePostPipelineBlock(Func<Task> taskFactory, int priority)
        {
            if (priority < 0 || (priority > 100 && priority < 200) || priority > 300)
            {
                throw new ArgumentOutOfRangeException("Priority must be between 0 and 100 or between 200 and 300");
            }

            TaskFactory = taskFactory;
            Priority = priority;
        }

        public Func<Task> TaskFactory { get; set; }
        public int Priority { get; set; } 
    }
}
