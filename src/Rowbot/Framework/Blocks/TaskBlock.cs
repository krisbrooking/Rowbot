using Rowbot.Framework.Pipelines.Summary;

namespace Rowbot.Framework.Blocks
{
    public sealed class TaskBlock : IBlock
    {
        private readonly Func<Task> _task;
        private BlockContext _blockContext = new();

        public TaskBlock(Func<Task> task)
        {
            _task = task;
        }

        public Func<Task> PrepareTask(BlockContext context)
        {
            _blockContext = context;

            return async () =>
            {
                var blockSummary = new BlockSummary<TaskBlock>();

                try
                {
                    await _task();
                }
                catch (Exception ex)
                {
                    blockSummary.Exceptions.TryAdd(ex.Message, (ex, blockSummary.TotalBatches));
                }

                _blockContext.SummaryCallback?.Invoke(blockSummary);
            };
        }
    }
}
