# Pipeline

## Background
`Pipeline` was created as the return type for pipeline builder. Its orginal purpose was nothing more than to store a list of blocks and pass them to the pipeline runner. `Pipeline` has since been given additional responsibilities in an effort to better separate concerns between it, pipeline builder, and pipeline runner.

- Pipeline builder is responsible for creating blocks and constructing a `Pipeline`.
- `Pipeline` is responsible for linking the blocks together in series and executing the blocks when requested to do so by pipeline runner.
- Pipeline runner is responsible for orchestrating the execution of multiple pipelines.

## Design

### Block Linker
The extract, transform, and load block types implement `IBlockSource<T>` or `IBlockTarget<T>` or both. A block implementing `IBlockSource<T>` can link to another block implementing `IBlockTarget<T>` using the `LinkTo()` method. Because the generic argument type `T` isn't known at compile time, we can't cast to `ISourceBlock<T>` to call the `LinkTo()` method and require reflection.

`BlockLinker` takes a preorderded list of blocks and uses reflection to access and invoke the `LinkTo()` method of each `ISourceBlock<T>` in order to link the list of blocks in series.

`BlockLinker` returns a stack of list of task factories `Stack<List<Func<Task>>>`. Every block has a `PrepareTask()` method which returns a `Func<Task>`. When blocks are linked in series they become a `List<Func<Task>>`. The stack exists because `TaskBlock` does not implement `ISourceBlock<T>` or `ITargetBlock<T>` and so is not linked to other blocks., Instead it is added as a single element `List<Func<Task>>`. In this way, the block linker can describe a task block that should run before or after the linked ETL blocks.

### Pipeline Execution
Given that every block constructs a `Func<Task>`, pipelines are executed asynchronously. Blocks of type extract, transform, and load, are started simultaneously and awaited using `Task.WhenAll()`. All tasks need to be running concurrently to support batch processing using the producer/consumer pattern. A block's channel manages when a block's `Task` is complete and ready to be shutdown. Once all channels have completed, the aggregate `Task` completes.

### Pipeline Summary
A `Pipeline` returns a `PipelineSummary` after execution with metrics related to block completion status including errors, runtime, and rows affected. This information is considered separate from logging in that it gives the developer a high level understanding of the last run, rather than information that can be used to trace a problem.

Because blocks run concurrently, callbacks are used to collect data. Before the pipeline is executed, each block is configured with a callback function that writes a summary to a `ConcurrentBag<IBlockSummary>`. When a block completes, it invokes the callback function to send its summary back to the `Pipeline`.

After all blocks complete, the block summaries are aggregated into a `PipelineSummary`.