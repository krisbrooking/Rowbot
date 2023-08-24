using Microsoft.Extensions.Logging;
using Rowbot.Framework.Blocks.Extractors.Parameters;
using Rowbot.Framework.Pipelines.Summary;
using System.Threading.Channels;

namespace Rowbot.Framework.Blocks
{
    public sealed class ExtractBlock<TOutput> : IBlockSource<TOutput>
    {
        private readonly IExtractor<TOutput> _source;
        private readonly ILoggerFactory _loggerFactory;
        private readonly CancellationToken _cancellationToken;
        private BlockContext _blockContext = new();

        public ExtractBlock(IExtractor<TOutput> source, ILoggerFactory loggerFactory, CancellationToken token = default)
        {
            _source = source;
            _loggerFactory = loggerFactory;
            _cancellationToken = token;
        }

        public ChannelWriter<TOutput[]>? WriterOut { get; set; }

        public Func<Task> PrepareTask(BlockContext context)
        {
            _blockContext = context;

            return async () =>
            {
                var workerCount = 1;
                var workers = Enumerable.Range(0, workerCount)
                    .Select(_ => Task.Run(async () =>
                    {
                        if (WriterOut is null)
                        {
                            return;
                        }

                        var logger = _loggerFactory.CreateLogger<ExtractBlock<TOutput>>();
                        var blockSummary = new BlockSummary<ExtractBlock<TOutput>>();

                        try
                        {
                            var result = new List<TOutput>(_blockContext.ExtractorOptions.BatchSize);

                            await foreach (var userParameters in _blockContext.ExtractorOptions.ExtractParameterGenerator.GenerateAsync())
                            await foreach (var item in _source.ExtractAsync(userParameters.AddBatchSizeParameter(_blockContext.ExtractorOptions.BatchSize), _cancellationToken))
                            {
                                result.Add(item);
                                if (result.Count == _blockContext.ExtractorOptions.BatchSize)
                                {
                                    await WriterOut.WriteAsync(result.ToArray()).ConfigureAwait(false);
                                    result = new List<TOutput>(_blockContext.ExtractorOptions.BatchSize);

                                    blockSummary.TotalBatches++;
                                    blockSummary.RowsExtracted += _blockContext.ExtractorOptions.BatchSize;
                                }
                            }

                            if (result.Count > 0)
                            {
                                await WriterOut.WriteAsync(result.ToArray()).ConfigureAwait(false);

                                blockSummary.TotalBatches++;
                                blockSummary.RowsExtracted += result.Count;
                            }
                        }
                        catch (Exception ex)
                        {
                            blockSummary.Exceptions.TryAdd(ex.Message, (ex, blockSummary.TotalBatches));
                            logger.LogError(ex, ex.Message);
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
