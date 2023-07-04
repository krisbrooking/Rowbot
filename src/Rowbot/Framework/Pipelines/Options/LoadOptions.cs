namespace Rowbot.Framework.Pipelines.Options
{
    public sealed class LoadOptions
    {
        public LoadOptions() { }

        public LoadOptions(int workerCount)
        {
            WorkerCount = workerCount;
        }

        public int WorkerCount { get; set; } = 1;
        public int MaxExceptions { get; set; } = 3;
    }
}
