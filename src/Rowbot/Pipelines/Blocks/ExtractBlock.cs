using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Rowbot.Extractors.Framework;
using Rowbot.Pipelines.Summary;

namespace Rowbot.Pipelines.Blocks;

public sealed class ExtractBlock<TInput, TOutput> : IBlockSource<TOutput>
{
    private readonly IExtractor<TInput, TOutput> _extractor;
    private readonly ILoggerFactory _loggerFactory;
    private readonly int _batchSize;
    private readonly BlockOptions _blockOptions;
    private readonly CancellationToken _cancellationToken;
    private int _exceptionCount;

    public ExtractBlock(
        IExtractor<TInput, TOutput> extractor, 
        ILoggerFactory loggerFactory, 
        int batchSize,
        BlockOptions blockOptions,
        CancellationToken token = default)
    {
        _extractor = extractor;
        _loggerFactory = loggerFactory;
        _batchSize = batchSize;
        _blockOptions = blockOptions;
        _cancellationToken = token;
    }

    public ChannelWriter<TOutput[]>? WriterOut { get; private set; }
    public Action<BlockSummary>? SummaryCallback { get; set; }

    public Func<Task> PrepareTask()
    {
        _exceptionCount = 0;
        
        return async () =>
        {
            var workers = Enumerable.Range(0, _blockOptions.WorkerCount)
                .Select(_ => Task.Run(async () =>
                {
                    if (WriterOut is null)
                    {
                        return;
                    }

                    var logger = _loggerFactory.CreateLogger<ExtractBlock<TInput, TOutput>>();
                    var blockSummary = BlockSummaryFactory.Create<ExtractBlock<TInput, TOutput>>();

                    try
                    {
                        var result = new List<TOutput>(_batchSize);

                        await foreach (var output in _extractor.ExtractAsync(new ExtractContext<TInput>(_batchSize), _cancellationToken))
                        {
                            result.Add(output);
                            if (result.Count == _batchSize)
                            {
                                await WriteAsync(result, blockSummary).ConfigureAwait(false);
                                result = new List<TOutput>(_batchSize);
                            }
                        }

                        if (result.Count > 0)
                        {
                            await WriteAsync(result, blockSummary).ConfigureAwait(false);
                            result = new List<TOutput>(_batchSize);
                        }
                    }
                    catch (Exception ex)
                    {
                        blockSummary.Exceptions.TryAdd(ex.Message, (ex, blockSummary.TotalBatches));
                        logger.LogError(ex, ex.Message);
                        if (_exceptionCount++ == _blockOptions.MaxExceptions)
                        {
                            logger.LogError("Max exceptions reached, exiting worker");
                            return;
                        }
                    }

                    SummaryCallback?.Invoke(blockSummary);
                }))
                .ToArray();

            await Task.WhenAll(workers)
                .ContinueWith((_) => WriterOut?.Complete(), _cancellationToken)
                .ConfigureAwait(false);
        };
    }

    private async Task WriteAsync(List<TOutput> result, BlockSummary blockSummary)
    {
        if (WriterOut is null)
        {
            return;
        }
        
        await WriterOut.WriteAsync(result.ToArray(), _cancellationToken)
            .ConfigureAwait(false);

        blockSummary.TotalBatches++;
        blockSummary.RowsExtracted += result.Count;
    }

    public void LinkTo(IBlockTarget<TOutput> target)
    {
        WriterOut = target.WriterIn;
    }
}