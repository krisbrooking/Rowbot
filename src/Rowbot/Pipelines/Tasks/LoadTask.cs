using Rowbot.Pipelines.Summary;

namespace Rowbot.Pipelines.Tasks;

public sealed class LoadTask<TInput, TConnector>
    : ITask
    where TConnector : IWriteConnector<TInput>
{
    private readonly TConnector _connector;
    private readonly Func<TConnector, Task>? _asyncTask;
    private readonly Action<TConnector>? _task;
    private readonly string _name;

    public LoadTask(TConnector connector, Func<TConnector, Task> asyncTask, string name)
    {
        _connector = connector;
        _asyncTask = asyncTask;
        _name = name;
    }
    
    public LoadTask(TConnector connector, Action<TConnector> task, string name)
    {
        _connector = connector;
        _task = task;
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
            await _asyncTask(_connector);
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
            _task(_connector);
        }
        catch (Exception ex)
        {
            summary.Exceptions.TryAdd(ex.Message, (ex, 1));
        }
        
        return summary;
    }
}