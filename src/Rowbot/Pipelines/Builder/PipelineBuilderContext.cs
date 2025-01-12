using Microsoft.Extensions.Logging;
using Rowbot.Pipelines.Blocks;
using Rowbot.Pipelines.Runner.DependencyResolution;
using Rowbot.Pipelines.Tasks;

namespace Rowbot.Pipelines.Builder;

/// <summary>
/// Shared data passed between pipeline builder stages.
/// </summary>
/// <remarks>
/// The pipeline builder stages after <see cref="PipelineBuilder"/> don't support dependency injection 
/// so dependencies are injected in <see cref="PipelineBuilder"/> and passed between stages using <see cref="PipelineBuilderContext"/>
/// </remarks>
public sealed class PipelineBuilderContext(ILoggerFactory loggerFactory, ServiceFactory genericServiceFactory)
{
    private Dictionary<(TaskExecutionOrder, TaskPriority), int> _taskCount = ResetTaskCount();

    private PipelineBuilderContext(PipelineBuilderContext context) : this(context.LoggerFactory, context.ServiceFactory)
    {
        Blocks = context.Blocks;
        PrePipelineTasks = context.PrePipelineTasks;
        PostPipelineTasks = context.PostPipelineTasks;
        DependencyResolution = context.DependencyResolution;
    }
    
    public ILoggerFactory LoggerFactory { get; } = loggerFactory;
    public ServiceFactory ServiceFactory { get; } = genericServiceFactory;
    public Queue<IBlock> Blocks { get; private set; } = new();
    public PriorityQueue<ITask, int> PrePipelineTasks { get; private set; } = new();
    public PriorityQueue<ITask, int> PostPipelineTasks { get; private set; } = new();
    public DependencyResolutionMetadata DependencyResolution { get; private set; } = new();

    public void EnqueueTask(ITask task, TaskExecutionOrder taskExecutionOrder, TaskPriority priority)
    {
        if (taskExecutionOrder == TaskExecutionOrder.PrePipeline)
        {
            PrePipelineTasks.Enqueue(task, (int)priority + _taskCount[(taskExecutionOrder, priority)]++);
        }
        else
        {
            PostPipelineTasks.Enqueue(task, (int)priority + _taskCount[(taskExecutionOrder, priority)]++);
        }
    }

    /// <summary>
    /// Creates a copy of this instance for the pipeline.
    /// </summary>
    public PipelineBuilderContext Clone() => new(this);
    
    /// <summary>
    /// Resets this object instance. Allows a single instance of <see cref="PipelineBuilder"/> to create multiple pipelines.
    /// </summary>
    public void Reset()
    {
        _taskCount = ResetTaskCount();
        Blocks = new();
        PrePipelineTasks =new();
        PostPipelineTasks = new();
        DependencyResolution = new();
    }

    private static Dictionary<(TaskExecutionOrder, TaskPriority), int> ResetTaskCount() =>
        new Dictionary<(TaskExecutionOrder, TaskPriority), int>()
        {
            { (TaskExecutionOrder.PrePipeline, TaskPriority.High), 0 },
            { (TaskExecutionOrder.PrePipeline, TaskPriority.Medium), 0 },
            { (TaskExecutionOrder.PrePipeline, TaskPriority.Low), 0 },
            { (TaskExecutionOrder.PostPipeline, TaskPriority.High), 0 },
            { (TaskExecutionOrder.PostPipeline, TaskPriority.Medium), 0 },
            { (TaskExecutionOrder.PostPipeline, TaskPriority.Low), 0 }
        };
}