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
    private readonly CancellationToken _cancellationToken;
    private int _exceptionCount;

    public DeferredExtractBlock(
        Func<IExtractBuilder<TInput, TOutput>, TInput, IExtractBuilderExtractorStep<TInput, TOutput>> extractBuilder, 
        ServiceFactory serviceFactory,
        ILoggerFactory loggerFactory, 
        ExtractOptions blockOptions,
        CancellationToken token = default)
    {
        _extractBuilder = extractBuilder;
        _serviceFactory = serviceFactory;
        _loggerFactory = loggerFactory;
        _blockOptions = blockOptions;
        _cancellationToken = token;

        var channel = Channel.CreateBounded<TInput[]>(blockOptions.ChannelBoundedCapacity);
        Reader = channel.Reader;
        WriterIn = channel.Writer;
    }

    public ChannelWriter<TInput[]> WriterIn { get; }
    public ChannelReader<TInput[]> Reader { get; }
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

                    var logger = _loggerFactory.CreateLogger<DeferredExtractBlock<TInput, TOutput>>();
                    var blockSummary = BlockSummaryFactory.Create<DeferredExtractBlock<TInput, TOutput>>();

                    var context = new PipelineBuilderContext(_loggerFactory, _serviceFactory);
                    var extractBuilder = new DeferredExtractBuilder<TInput, TOutput>(context);

                    try
                    {
                        var result = new List<TOutput>(_blockOptions.BatchSize);

                        await foreach (var items in Reader.ReadAllAsync(_cancellationToken))
                        {
                            foreach (var item in items)
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
                                                   _cancellationToken))
                                {
                                    result.Add(output);
                                    if (result.Count == _blockOptions.BatchSize)
                                    {
                                        await WriteAsync(result, blockSummary).ConfigureAwait(false);
                                        result = new List<TOutput>(_blockOptions.BatchSize);
                                    }
                                }
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
