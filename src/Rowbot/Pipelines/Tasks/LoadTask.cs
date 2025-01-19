using Microsoft.Extensions.Logging;
using Rowbot.Pipelines.Summary;

namespace Rowbot.Pipelines.Tasks;

public sealed class LoadTask<TInput, TConnector>
    : ITask
    where TConnector : IWriteConnector<TInput>
{
    private readonly TConnector _connector;
    private readonly Func<TConnector, ILogger<LoadTask<TInput, TConnector>>, Task>? _asyncTask;
    private readonly Action<TConnector, ILogger<LoadTask<TInput, TConnector>>>? _task;
    private readonly ILoggerFactory _loggerFactory;
    private readonly string _name;

    public LoadTask(TConnector connector, Func<TConnector, ILogger<LoadTask<TInput, TConnector>>, Task> asyncTask, ILoggerFactory loggerFactory, string name)
    {
        _connector = connector;
        _asyncTask = asyncTask;
        _loggerFactory = loggerFactory;
        _name = name;
    }
    
    public LoadTask(TConnector connector, Action<TConnector, ILogger<LoadTask<TInput, TConnector>>> task, ILoggerFactory loggerFactory, string name)
    {
        _connector = connector;
        _task = task;
        _loggerFactory = loggerFactory;
        _name = name;
    }
    
    public bool IsAsync => _asyncTask is not null;

    public async Task<BlockSummary> RunAsync()
    {
        if (_asyncTask is null)
        {
            throw new InvalidOperationException("No async task configured.");
        }

        var summary = new BlockSummary(_name);
        try
        {
            await _asyncTask(_connector, _loggerFactory.CreateLogger<LoadTask<TInput, TConnector>>());
        }
        catch (Exception ex)
        {
            summary.Exceptions.TryAdd(ex.Message, (ex, 1));
        }

        return summary;
    }

    public BlockSummary Run()
    {
        if (_task is null)
        {
            throw new InvalidOperationException("No task configured.");
        }
        
        var summary = new BlockSummary(_name);
        try
        {
            _task(_connector, _loggerFactory.CreateLogger<LoadTask<TInput, TConnector>>());
        }
        catch (Exception ex)
        {
            summary.Exceptions.TryAdd(ex.Message, (ex, 1));
        }
        
        return summary;
    }
}