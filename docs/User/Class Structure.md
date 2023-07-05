# Class Structure

## Pipeline Container
A pipeline is declared inside a pipeline container, a class that implements `IPipelineContainer`. Any method within the class that returns an object of type `Pipeline` will be registered for execution by the pipeline runner. A pipeline container can contain any number of such methods.

```csharp
public sealed class CustomerPipelines : IPipelineContainer
{
    public Pipeline Stage() { ... }
    public Pipeline Load() { ... }
}
```

### Declaring Dependencies
Dependencies are declared using `IPipelineBuilder`. The `DependsOn<TDependsOn>()` method can be called prior to the extract stage. The generic argument `TDependsOn` is set to an entity class that is loaded by another pipeline.

In the following example, loading customer data into the data warehouse depends first on source data being loaded into a staging environment. The pipeline runner, when given this configuration, will create two groups of pipelines, one for the pipeline defined in the `Stage()` method and one for the pipeline defined in the `Load()` method, and run one after the other.

```csharp
public class CustomerPipelines : IPipelineContainer
{
    private readonly IPipelineBuilder _pipelineBuilder;

    public CustomerPipeline(IPipelineBuilder pipelineBuilder) { _pipelineBuilder = pipelineBuilder; }

    public Pipeline Stage()
    {
        return _pipelineBuilder
            .Extract<SourceCustomer>()    // Extract customers from source system
            .Load<StagingCustomer>();      // Load source data into staging database
    }

    public Pipeline Load()
    {
        return _pipelineBuilder
            .DependsOn<StagingCustomer>() // This pipeline depends on StagingCustomer being loaded first
            .Extract<StagingCustomer>()   // Extract customers from staging
            .Transform<TargetCustomer>()  // Load customers to the data warehouse
            .Load();
    }
}
```

### Tagging
Individual pipelines can be tagged by decorating the pipeline method with a `Tag` attribute. The attribute accepts one or more tags as a comma delimited list of strings.

The `RunAsync()` method of `IPipelineRunner` supports filtering pipelines to run by tag or cluster.

```csharp
public sealed class CustomerPipelines : IPipelineContainer
{
    [Tag("Stage", "StageCustomers")]
    public Pipeline StageCustomers() { ... }
    [Tag("Load")]
    public Pipeline LoadCustomers() { ... }
}

public sealed class ProductsPipelines : IPipelineContainer
{
    [Tag("Stage", "StageProducts")]
    public Pipeline StageProducts() { ... }
    [Tag("Load")]
    public Pipeline LoadProducts() { ... }
}

await _pipelineRunner.RunAsync(pipelines => pipelines.FilterByTag("Stage"));
```

### Clustering
Pipeline containers can be decorated with a `Cluster` attribute to logically isolate containers. The pipeline runner will run clusters of pipelines concurrently. If no cluster is specified, pipelines are placed in a "Default" cluster.

In the following example, Ecommerce and Accounts pipelines will run concurrently. This only works when there are no dependencies present between pipelines in these two clusters. Rowbot does not detect dependencies between clusters and therefore does not warn the user of potential issues.

> Think carefully about whether pipelines can run concurrently to avoid issues when loading data.

```csharp
[Cluster("Ecommerce")]
public sealed class CustomerPipelines : IPipelineContainer
{
    public Pipeline Stage() { ... }
    public Pipeline Load() { ... }
}

[Cluster("Accounts")]
public sealed class CustomerPipelines : IPipelineContainer
{
    public Pipeline Stage() { ... }
    public Pipeline Load() { ... }
}
```

## Suggested Structure
One suggested approach to class design in Rowbot is to group data pipelines by entity. This is shown in the above examples where customers and products exist in separate classes, as do customers from the Ecommerce store and customers from the account management system.

Within a class, there are data pipelines for every phase of the data load. This might include:

- Collect data from source
- Load inserts and updates
- Load deletes

The pipeline runner is responsible for executing these data pipelines in the correct order. They do not need to appear in the pipeline container class in any particular order.

### Constructor Injection
Every pipeline container must inject `IPipelineBuilder` to support building pipelines. A pipeline container will typically also require logging and configuration from an external source.

```csharp
public class CustomerPipelines : IPipelineContainer
{
    private readonly IPipelineBuilder _pipelineBuilder;
    private readonly ILogger<CustomerPipelines> _logger;
    private readonly CustomerOptions _options;

    public CustomerPipeline(
        IPipelineBuilder pipelineBuilder, 
        ILogger<CustomerPipelines> logger,
        IOptions<CustomerOptions> options)
    {
        _pipelineBuilder = pipelineBuilder;
        _logger = logger;
        _options = options.Value;
    }
}
```