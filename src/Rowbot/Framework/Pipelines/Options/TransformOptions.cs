namespace Rowbot.Framework.Pipelines.Options
{
    public sealed class TransformOptions
    {
        public TransformOptions() { }

        public TransformOptions(int workerCount)
        {
            WorkerCount = workerCount;
        }

        public int WorkerCount { get; set; } = 1;
        public int MaxExceptions { get; set; } = 3;
    }
}
