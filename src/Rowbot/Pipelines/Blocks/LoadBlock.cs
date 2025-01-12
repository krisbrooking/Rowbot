using Microsoft.Extensions.Logging;
using Rowbot.Pipelines.Summary;
using System.Threading.Channels;

namespace Rowbot.Pipelines.Blocks;

public class LoadBlock<TInput> : IBlockTarget<TInput>
{
    private readonly ILoader<TInput> _loader;
    private readonly ILoggerFactory _loggerFactory;
    private readonly BlockOptions _blockOptions;
    private int _exceptionCount;
    
    public LoadBlock(
        ILoader<TInput> loader, 
        ILoggerFactory loggerFactory,
        BlockOptions blockOptions)
    {
        _loader = loader;
        _loggerFactory = loggerFactory;
        _blockOptions = blockOptions;

        var channel = Channel.CreateBounded<TInput[]>(blockOptions.BoundedCapacity);
        Reader = channel.Reader;
        WriterIn = channel.Writer;
    }

    public ChannelWriter<TInput[]> WriterIn { get; }
    public ChannelReader<TInput[]> Reader { get; }
    public Action<BlockSummary>? SummaryCallback { get; set; }

    public Func<Task> PrepareTask()
    {
        _exceptionCount = 0;
        
        return async () =>
        {
            var workers = Enumerable.Range(0, _blockOptions.WorkerCount)
                .Select(_ => Task.Run(async () =>
                {
                    var logger = _loggerFactory.CreateLogger<LoadBlock<TInput>>();
                    var blockSummary = BlockSummaryFactory.Create<LoadBlock<TInput>>();

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
                            if (_exceptionCount++ == _blockOptions.MaxExceptions)
                            {
                                logger.LogError("Max exceptions reached, exiting worker");
                                return;
                            }
                        }
                    }

                    SummaryCallback?.Invoke(blockSummary);
                }))
                .ToArray();

            await Task.WhenAll(workers);
        };
    }
}
