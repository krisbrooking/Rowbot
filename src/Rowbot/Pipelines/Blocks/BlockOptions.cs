namespace Rowbot.Pipelines.Blocks;

public sealed record BlockOptions(
    int WorkerCount = 1,
    int MaxExceptions = 3,
    int BoundedCapacity = 1);