using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Rowbot.Extractors.Framework;
using Rowbot.Pipelines.Summary;

namespace Rowbot.Pipelines.Blocks;

public sealed class PrimaryExtractBlock<TInput, TOutput> : IBlockSource<TOutput>
{
    private readonly IExtractor<TInput, TOutput> _extractor;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ExtractParameter[] _parameters;
    private readonly ExtractOptions _blockOptions;
    private readonly CancellationToken _cancellationToken;
    private int _exceptionCount;

    public PrimaryExtractBlock(
        IExtractor<TInput, TOutput> extractor, 
        ILoggerFactory loggerFactory,
        ExtractParameter[] parameters,
        ExtractOptions blockOptions,
        CancellationToken token = default)
    {
        _extractor = extractor;
        _loggerFactory = loggerFactory;
        _parameters = parameters;
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
            var workers = new List<Task>();
            
            for (var i = 0; i < _blockOptions.WorkerCount; i++)
            {
                workers.Add(RunTaskAsync());
            }

            await Task.WhenAll(workers)
                .ContinueWith((_) => WriterOut?.Complete(), _cancellationToken)
                .ConfigureAwait(false);
        };
    }

    private async Task RunTaskAsync()
    {
        if (WriterOut is null)
        {
            return;
        }

        var logger = _loggerFactory.CreateLogger<PrimaryExtractBlock<TInput, TOutput>>();
        var blockSummary = BlockSummaryFactory.Create<PrimaryExtractBlock<TInput, TOutput>>();
        
        try
        {
            var result = new List<TOutput>(_blockOptions.BatchSize);

            await foreach (var output in _extractor.ExtractAsync(new ExtractContext<TInput>(_blockOptions.BatchSize, _parameters), _cancellationToken))
            {
                result.Add(output);
                if (result.Count == _blockOptions.BatchSize)
                {
                    await WriteAsync(result, blockSummary).ConfigureAwait(false);
                    result = new List<TOutput>(_blockOptions.BatchSize);
                }
            }

            if (result.Count > 0)
            {
                await WriteAsync(result, blockSummary).ConfigureAwait(false);
                result = new List<TOutput>(_blockOptions.BatchSize);
            }
        }
        catch (Exception ex)
        {
            blockSummary.Exceptions.TryAdd(ex.Message, (ex, blockSummary.TotalBatches));
            logger.LogError(ex, ex.Message);
            if (_exceptionCount++ >= _blockOptions.MaxExceptions)
            {
                logger.LogError("Max exceptions reached, exiting worker");
                return;
            }
        }

        SummaryCallback?.Invoke(blockSummary);
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