using Microsoft.Extensions.Logging;
using Rowbot.Framework.Pipelines.Summary;
using System.Threading.Channels;

namespace Rowbot.Framework.Blocks
{
    public class TransformBlock<TInput, TOutput> : IBlockTarget<TInput>, IBlockSource<TOutput>
    {
        private readonly ITransformer<TInput, TOutput>? _transformer;
        private readonly ISynchronousTransformer<TInput, TOutput>? _synchronousTransformer;
        private readonly ILoggerFactory _loggerFactory;
        private BlockContext _blockContext = new();

        public TransformBlock(ITransformer<TInput, TOutput> transformer, ILoggerFactory loggerFactory, int boundedCapacity)
        {
            _transformer = transformer;
            _loggerFactory = loggerFactory;

            var channel = Channel.CreateBounded<TInput[]>(boundedCapacity);
            Reader = channel.Reader;
            WriterIn = channel.Writer;
        }

        internal TransformBlock(ISynchronousTransformer<TInput, TOutput> synchronousTransformer, ILoggerFactory loggerFactory, int boundedCapacity)
        {
            _synchronousTransformer = synchronousTransformer;
            _loggerFactory = loggerFactory;

            var channel = Channel.CreateBounded<TInput[]>(boundedCapacity);
            Reader = channel.Reader;
            WriterIn = channel.Writer;
        }

        public ChannelWriter<TInput[]> WriterIn { get; }
        public ChannelReader<TInput[]> Reader { get; }
        public ChannelWriter<TOutput[]>? WriterOut { get; set; }

        public Func<Task> PrepareTask(BlockContext context)
        {
            _blockContext = context;

            return async () =>
            {
                var workers = Enumerable.Range(0, _blockContext.TransformerOptions.WorkerCount)
                    .Select(_ => Task.Run(async () =>
                    {
                        var logger = _loggerFactory.CreateLogger<TransformBlock<TInput, TOutput>>();
                        var blockSummary = new BlockSummary<TransformBlock<TInput, TOutput>>();
                        var exceptionCount = 0;

                        await foreach (var item in Reader.ReadAllAsync().ConfigureAwait(false))
                        {
                            try
                            {
                                var result = _transformer is not null ? await _transformer.TransformAsync(item).ConfigureAwait(false) : _synchronousTransformer!.Transform(item);
                                if (WriterOut != null)
                                {
                                    await WriterOut.WriteAsync(result).ConfigureAwait(false);

                                    blockSummary.TotalBatches++;
                                    blockSummary.RowsTransformed += result.Count();
                                }
                            }
                            catch (Exception ex)
                            {
                                blockSummary.Exceptions.TryAdd(ex.Message, (ex, blockSummary.TotalBatches));
                                logger.LogError(ex, ex.Message);
                                if (exceptionCount++ == _blockContext.TransformerOptions.MaxExceptions)
                                {
                                    logger.LogError("Max exceptions reached, exiting worker");
                                    return;
                                }
                            }
                        }

                        _blockContext.SummaryCallback?.Invoke(blockSummary);
                    }))
                    .ToArray();

                await Task.WhenAll(workers)
                    .ContinueWith((_) => WriterOut?.Complete())
                    .ConfigureAwait(false);
            };
        }

        public void LinkTo(IBlockTarget<TOutput> target)
        {
            WriterOut = target.WriterIn;
        }
    }
}
