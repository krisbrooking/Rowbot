namespace Rowbot;

public abstract class BlockOptions
{
    public int WorkerCount { get; set; } = 1;
    public int MaxExceptions { get; set; } = 3;
    public int ChannelBoundedCapacity { get; set; } = 1;
}

public sealed class ExtractOptions : BlockOptions
{
    public ExtractOptions() { }

    public ExtractOptions(int batchSize)
    {
        BatchSize = batchSize;
    }

    public ExtractOptions(int batchSize, int workerCount)
    {
        BatchSize = batchSize;
        WorkerCount = workerCount;
    }

    public ExtractOptions(int batchSize, int workerCount, int maxExceptions)
    {
        BatchSize = batchSize;
        WorkerCount = workerCount;
        MaxExceptions = maxExceptions;
    }

    public int BatchSize { get; set; } = 1000;
}

public sealed class TransformOptions : BlockOptions
{
    public TransformOptions() { }

    public TransformOptions(int workerCount)
    {
        WorkerCount = workerCount;
    }

    public TransformOptions(int workerCount, int maxExceptions)
    {
        WorkerCount = workerCount;
        MaxExceptions = maxExceptions;
    }
}

public sealed class LoadOptions : BlockOptions
{
    public LoadOptions() { }

    public LoadOptions(int workerCount)
    {
        WorkerCount = workerCount;
    }

    public LoadOptions(int workerCount, int maxExceptions)
    {
        WorkerCount = workerCount;
        MaxExceptions = maxExceptions;
    }
}