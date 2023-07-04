using Microsoft.Extensions.Logging;
using Rowbot.Framework.Pipelines.Summary;
using System.Threading.Channels;

namespace Rowbot.Framework.Blocks
{
    public class LoadBlock<TInput> : IBlockTarget<TInput>
    {
        private readonly ILoader<TInput> _loader;
        private readonly ILoggerFactory _loggerFactory;
        private BlockContext _blockContext = new();

        public LoadBlock(ILoader<TInput> loader, ILoggerFactory loggerFactory, int boundedCapacity)
        {
            _loader = loader;
            _loggerFactory = loggerFactory;

            var channel = Channel.CreateBounded<TInput[]>(boundedCapacity);
            Reader = channel.Reader;
            WriterIn = channel.Writer;
        }

        public ChannelWriter<TInput[]> WriterIn { get; }
        public ChannelReader<TInput[]> Reader { get; }

        public Func<Task> PrepareTask(BlockContext context)
        {
            _blockContext = context;

            return async () =>
            {
                var workers = Enumerable.Range(0, _blockContext.LoadOptions.WorkerCount)
                    .Select(_ => Task.Run(async () =>
                    {
                        var logger = _loggerFactory.CreateLogger<LoadBlock<TInput>>();
                        var blockSummary = new BlockSummary<LoadBlock<TInput>>();
                        var exceptionCount = 0;

                        await foreach (var item in Reader.ReadAllAsync().ConfigureAwait(false))
                        {
                            try
                            {
                                var result = await _loader.LoadAsync(item).ConfigureAwait(false);

                                blockSummary.TotalBatches++;
                                blockSummary.RowsInserted += result.Inserts.Count();
                                blockSummary.RowsUpdated += result.Updates.Count();
                            }
                            catch (Exception ex)
                            {
                                blockSummary.Exceptions.TryAdd(ex.Message, (ex, blockSummary.TotalBatches));
                                logger.LogError(ex, ex.Message);
                                if (exceptionCount++ == _blockContext.LoadOptions.MaxExceptions)
                                {
                                    logger.LogError("Max exceptions reached, exiting worker");
                                    return;
                                }
                            }
                        }

                        _blockContext.SummaryCallback?.Invoke(blockSummary);
                    }))
                    .ToArray();

                await Task.WhenAll(workers);
            };
        }
    }
}
