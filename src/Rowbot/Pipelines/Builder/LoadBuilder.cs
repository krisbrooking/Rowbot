using Rowbot.Loaders;
using Rowbot.Loaders.Framework;
using Rowbot.Pipelines.Blocks;
using Rowbot.Pipelines.Builder;
using Rowbot.Pipelines.Tasks;

namespace Rowbot;

public interface ILoadBuilder<TInput>
{
    /// <summary>
    /// Adds a write connector to the pipeline.
    /// </summary>
    /// <param name="configure">Write connector configuration</param>
    /// <typeparam name="TConnector">Write connector implementing <see cref="IWriteConnector{TInput}"/></typeparam>
    /// <returns>Load block builder at loader/task step</returns>
    ILoadBuilderConnectorStep<TInput, TConnector> WithConnector<TConnector>(Action<TConnector>? configure = null)
        where TConnector : IWriteConnector<TInput>;
}

public interface ILoadBuilderConnectorStep<TInput, TConnector>
    : ILoadBuilderLoaderStep<TInput>
    where TConnector : IWriteConnector<TInput>
{
    /// <summary>
    /// Adds an asynchronous task to the pipeline. A task is executed once, either before, or after,
    /// pipeline execution.
    /// </summary>
    /// <param name="task">Returns task to execute
    ///     <param name="task arg1">Write connector implementing <see cref="IWriteConnector{TInput}"/></param>
    /// </param>
    /// <param name="name">A name to identify the task in the pipeline summary</param>
    /// <param name="executionOrder">Configure task to execute before or after pipeline execution</param>
    /// <param name="priority">Task priority. Tasks with the same priority will be executed in the order they are added
    /// to the builder.</param>
    /// <returns>Load block builder at loader/task step</returns>
    ILoadBuilderConnectorStep<TInput, TConnector> WithTask(
        Func<TConnector, Task> task,
        string name = "Task",
        TaskExecutionOrder executionOrder = TaskExecutionOrder.PrePipeline, 
        TaskPriority priority = TaskPriority.Low);
    
    /// <summary>
    /// Adds a task to the pipeline. A task is executed once, either before, or after, pipeline execution.
    /// </summary>
    /// <param name="task">Configures task to execute
    ///     <param name="task arg1">Write connector implementing <see cref="IWriteConnector{TInput}"/></param>
    /// </param>
    /// <param name="name">A name to identify the task in the pipeline summary</param>
    /// <param name="executionOrder">Configure task to execute before or after pipeline execution</param>
    /// <param name="priority">Task priority. Tasks with the same priority will be executed in the order they are added
    /// to the builder.</param>
    /// <returns>Load block builder at loader/task step</returns>
    ILoadBuilderConnectorStep<TInput, TConnector> WithTask(
        Action<TConnector> task, 
        string name = "Task",
        TaskExecutionOrder executionOrder = TaskExecutionOrder.PrePipeline, 
        TaskPriority priority = TaskPriority.Low);
    
    /// <summary>
    /// Adds a loader to the pipeline.
    /// </summary>
    /// <param name="configure">Loader configuration</param>
    /// <typeparam name="TLoader">Loader implementing <see cref="ILoader{TInput}"/></typeparam>
    /// <returns>Completed load block builder</returns>
    ILoadBuilderLoaderStep<TInput> WithLoader<TLoader>(Action<TLoader>? configure = null)
        where TLoader : ILoader<TInput>;

    /// <summary>
    /// Adds an inline loader to the pipeline.
    /// </summary>
    /// <param name="inlineLoader">Returns items changed at the destination
    ///     <param name="inlineLoader arg1">Data to load</param>
    ///     <param name="inlineLoader arg2">Write connector</param>
    /// </param>
    /// <returns>Completed load block builder</returns>
    ILoadBuilderLoaderStep<TInput> WithLoader(
        Func<TInput[], IWriteConnector<TInput>, Task<LoadResult<TInput>>> inlineLoader);
}

public interface ILoadBuilderLoaderStep<TInput>
{
}

internal interface ILoadBuilderConnectorInternal<TInput>
{
    bool HasLoader { get; }
    ILoadBuilderLoaderStep<TInput> AddDefaultLoader();
}

public class LoadBuilder<TInput>(PipelineBuilderContext context)
    : ILoadBuilder<TInput>
{
    private readonly PipelineBuilderContext _context = context;
        
    public ILoadBuilderConnectorStep<TInput, TConnector> WithConnector<TConnector>(Action<TConnector>? configure = null) 
        where TConnector : IWriteConnector<TInput>
    {
        var writeConnector = _context.ServiceFactory.CreateWriteConnector<TConnector, TInput>();
        configure?.Invoke(writeConnector);
        
        return new LoadConnector<TInput, TConnector>(_context, writeConnector);
    }
}

public class LoadConnector<TInput, TConnector>(
    PipelineBuilderContext context, 
    TConnector writeConnector)
    : ILoadBuilderConnectorStep<TInput, TConnector>, ILoadBuilderConnectorInternal<TInput>
    where TConnector : IWriteConnector<TInput>
{
    private readonly PipelineBuilderContext _context = context;
    private readonly TConnector _writeConnector = writeConnector;
    
    public bool HasLoader { get; private set; }

    public ILoadBuilderConnectorStep<TInput, TConnector> WithTask(
        Func<TConnector, Task> task, 
        string name = "Task",
        TaskExecutionOrder executionOrder = TaskExecutionOrder.PrePipeline, 
        TaskPriority priority = TaskPriority.Low)
    {
        _context.EnqueueTask(new LoadTask<TInput, TConnector>(_writeConnector, task, name), executionOrder, priority);

        return this;
    }

    public ILoadBuilderConnectorStep<TInput, TConnector> WithTask(
        Action<TConnector> task, 
        string name = "Task",
        TaskExecutionOrder executionOrder = TaskExecutionOrder.PrePipeline, 
        TaskPriority priority = TaskPriority.Low)
    {
        _context.EnqueueTask(new LoadTask<TInput, TConnector>(_writeConnector, task, name), executionOrder, priority);

        return this;
    }

    public ILoadBuilderLoaderStep<TInput> WithLoader<TLoader>(Action<TLoader>? configure = null) 
        where TLoader : ILoader<TInput>
    {
        if (_writeConnector is null)
        {
            throw new InvalidOperationException("Write connector is not configured");
        }

        if (HasLoader)
        {
            return this;
        }
        
        _context.DependencyResolution.SetTargetEntity(typeof(TLoader));

        var loader = _context.ServiceFactory.CreateLoader<TLoader, TInput>(_writeConnector);
        configure?.Invoke(loader);

        _context.Blocks.Enqueue(new LoadBlock<TInput>(loader, _context.LoggerFactory, new BlockOptions()));
        
        HasLoader = true;

        if (_writeConnector is ICreateConnector)
        {
            _context.PrePipelineTasks.Enqueue(
                new LoadTask<TInput, TConnector>(
                    _writeConnector, 
                    async connector => await ((ICreateConnector)connector).CreateDataSetAsync(), 
                    "Create Data Set"),
                priority: 1);
        }

        return this;
    }

    public ILoadBuilderLoaderStep<TInput> WithLoader(
        Func<TInput[], IWriteConnector<TInput>, Task<LoadResult<TInput>>> inlineLoader)
        => WithLoader<InlineLoader<TInput>>(loader => loader.LoaderDelegate = inlineLoader);

    public ILoadBuilderLoaderStep<TInput> AddDefaultLoader() 
        => WithLoader<DefaultLoader<TInput>>();
}