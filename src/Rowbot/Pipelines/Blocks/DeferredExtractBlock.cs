using Microsoft.Extensions.Logging;
using Rowbot.Pipelines.Summary;
using System.Threading.Channels;
using Rowbot.Extractors.Framework;
using Rowbot.Pipelines.Builder;

namespace Rowbot.Pipelines.Blocks;

public sealed class DeferredExtractBlock<TInput, TOutput> : IBlockTarget<TInput>, IBlockSource<TOutput>
{
    private readonly Func<IExtractBuilder<TInput, TOutput>, TInput, IExtractBuilderExtractorStep<TInput, TOutput>> _extractBuilder;
    private readonly ServiceFactory _serviceFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ExtractOptions _blockOptions;
    private int _exceptionCount;

    public DeferredExtractBlock(
        Func<IExtractBuilder<TInput, TOutput>, TInput, IExtractBuilderExtractorStep<TInput, TOutput>> extractBuilder, 
        ServiceFactory serviceFactory,
        ILoggerFactory loggerFactory, 
        ExtractOptions blockOptions)
    {
        _extractBuilder = extractBuilder;
        _serviceFactory = serviceFactory;
        _loggerFactory = loggerFactory;
        _blockOptions = blockOptions;

        var channel = Channel.CreateBounded<TInput[]>(blockOptions.ChannelBoundedCapacity);
        Reader = channel.Reader;
        WriterIn = channel.Writer;
    }

    public ChannelWriter<TInput[]> WriterIn { get; }
    public ChannelReader<TInput[]> Reader { get; }
    public ChannelWriter<TOutput[]>? WriterOut { get; private set; }
    public Action<BlockSummary>? SummaryCallback { get; set; }

    public Func<CancellationTokenSource, Task> PrepareTask()
    {
        _exceptionCount = 0;
        
        return async (cts) =>
        {
            var workers = new List<Task>();
            
            for (var i = 0; i < _blockOptions.WorkerCount; i++)
            {
                workers.Add(RunTaskAsync(cts));
            }

            await Task.WhenAll(workers)
                .ContinueWith((_) => WriterOut?.Complete(), cts.Token)
                .ConfigureAwait(false);
        };
    }

    private async Task RunTaskAsync(CancellationTokenSource cts)
    {
        if (WriterOut is null)
        {
            return;
        }

        var logger = _loggerFactory.CreateLogger<DeferredExtractBlock<TInput, TOutput>>();
        var blockSummary = BlockSummaryFactory.Create<DeferredExtractBlock<TInput, TOutput>>();

        var context = new PipelineBuilderContext(_loggerFactory, _serviceFactory);
        var extractBuilder = new DeferredExtractBuilder<TInput, TOutput>(context);

        var result = new List<TOutput>(_blockOptions.BatchSize);

        await foreach (var items in Reader.ReadAllAsync(cts.Token).ConfigureAwait(false))
        {
            foreach (var item in items)
            {
                try
                {
                    var extractorStep = _extractBuilder(extractBuilder, item);

                    var connectorStep = (IExtractBuilderConnectorStepInternal<TInput, TOutput>)extractorStep;
                    if (!connectorStep.HasExtractor)
                    {
                        connectorStep.AddDefaultExtractor();
                    }

                    var extractConnector = (DeferredExtractConnector<TInput, TOutput>)connectorStep;

                    await foreach (var output in extractConnector.Extractor!.ExtractAsync(
                        new ExtractContext<TInput>(_blockOptions.BatchSize, item, extractConnector.Parameters),
                        cts.Token).ConfigureAwait(false))
                    {
                        result.Add(output);
                        if (result.Count == _blockOptions.BatchSize)
                        {
                            await WriteAsync(result, blockSummary, cts.Token).ConfigureAwait(false);
                            result = new List<TOutput>(_blockOptions.BatchSize);
                        }
                    }
                }
                catch (Exception ex)
                {
                    blockSummary.Exceptions.TryAdd(ex.Message, (ex, blockSummary.TotalBatches));
                    logger.LogError(ex, ex.Message);
                    if (_exceptionCount++ >= _blockOptions.MaxExceptions)
                    {
                        logger.LogError("Max exceptions reached, exiting worker");
                        cts.Cancel();
                        break;
                    }
                }
            }
        }

        if (result.Count > 0)
        {
            await WriteAsync(result, blockSummary, cts.Token).ConfigureAwait(false);
            result = new List<TOutput>(_blockOptions.BatchSize);
        }

        SummaryCallback?.Invoke(blockSummary);
    }

    private async Task WriteAsync(List<TOutput> result, BlockSummary blockSummary, CancellationToken cancellationToken)
    {
        if (WriterOut is null)
        {
            return;
        }
        
        await WriterOut.WriteAsync(result.ToArray(), cancellationToken)
            .ConfigureAwait(false);

        blockSummary.TotalBatches++;
        blockSummary.RowsExtracted += result.Count;
    }

    public void LinkTo(IBlockTarget<TOutput> target)
    {
        WriterOut = target.WriterIn;
    }
}
