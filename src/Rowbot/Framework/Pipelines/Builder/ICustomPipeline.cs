namespace Rowbot.Framework.Pipelines
{
    public interface ICustomPipeline
    {
        void AddPipelineBlock(Func<Task> prePipelineTaskFactory, int priority);
    }
}
