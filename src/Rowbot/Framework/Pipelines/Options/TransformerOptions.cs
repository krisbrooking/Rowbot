namespace Rowbot.Framework.Pipelines.Options
{
    public class TransformerOptions
    {
        public TransformerOptions() { }

        public TransformerOptions(int workerCount)
        {
            WorkerCount = workerCount;
        }

        public int WorkerCount { get; set; } = 1;
        public int MaxExceptions { get; set; } = 3;
    }
}
