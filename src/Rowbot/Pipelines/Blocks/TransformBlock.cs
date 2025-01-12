using Microsoft.Extensions.Logging;
using Rowbot.Pipelines.Summary;
using System.Threading.Channels;

namespace Rowbot.Pipelines.Blocks;

public class TransformBlock<TInput, TOutput> : IBlockTarget<TInput>, IBlockSource<TOutput>
{
    private readonly ITransformer<TInput, TOutput>? _transformer;
    private readonly IAsyncTransformer<TInput, TOutput>? _asyncTransformer;
    private readonly ILoggerFactory _loggerFactory;
    private readonly BlockOptions _blockOptions;
    private int _exceptionCount;

    public TransformBlock(
        ITransformer<TInput, TOutput> transformer, 
        ILoggerFactory loggerFactory,
        BlockOptions blockOptions)
    {
        _transformer = transformer;
        _loggerFactory = loggerFactory;
        _blockOptions = blockOptions;

        var channel = Channel.CreateBounded<TInput[]>(blockOptions.BoundedCapacity);
        Reader = channel.Reader;
        WriterIn = channel.Writer;
    }

    public TransformBlock(
        IAsyncTransformer<TInput, TOutput> transformer, 
        ILoggerFactory loggerFactory,
        BlockOptions blockOptions)
    {
        _asyncTransformer = transformer;
        _loggerFactory = loggerFactory;
        _blockOptions = blockOptions;

        var channel = Channel.CreateBounded<TInput[]>(blockOptions.BoundedCapacity);
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
                    var logger = _loggerFactory.CreateLogger<TransformBlock<TInput, TOutput>>();
                    var blockSummary = BlockSummaryFactory.Create<TransformBlock<TInput, TOutput>>();

                    await foreach (var item in Reader.ReadAllAsync().ConfigureAwait(false))
                    {
                        try
                        {
                            var result = (_transformer, _asyncTransformer) switch
                            {
                                (null, {}) => await _asyncTransformer.TransformAsync(item).ConfigureAwait(false),
                                ({}, null) => _transformer.Transform(item),
                                _ => throw new InvalidOperationException("No valid transformer configured")
                            };
                            
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
                            if (_exceptionCount++ >= _blockOptions.MaxExceptions)
                            {
                                logger.LogError("Max exceptions reached, exiting worker");
                                return;
                            }
                        }
                    }

                    SummaryCallback?.Invoke(blockSummary);
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