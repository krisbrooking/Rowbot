namespace Rowbot.Framework.Pipelines.Options
{
    public class LoaderOptions
    {
        public LoaderOptions() { }

        public LoaderOptions(int workerCount)
        {
            WorkerCount = workerCount;
        }

        public int WorkerCount { get; set; } = 1;
        public int MaxExceptions { get; set; } = 3;
    }
}
