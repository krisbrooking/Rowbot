# Pipeline Builder

## Background
The pipeline builder is responsible for ensuring that the user builds a pipeline as a series of blocks in the correct order.

The concept has remained stable over time but the api design has evolved. Originally, a pipeline definition was very concise.

```csharp
_pipelineBuilder
    .Extract(_sourceConnector)
    .Transform((source, mapper) => Transform(source, mapper))
    .Load(_targetConnector, _loader);
```

The connectors api was later changed to support an options class and extension methods rather than dependency injection. See [Contributor -> Connectors](Connectors.md) for a justification. 

The primary advantage of this approach from the perspective of the pipeline builder is that component configuration is much more visible within the data pipeline.

The example below provides more context into the data extraction and load operations that are going to be performed than the example above. There is no need to look to the top of the class for configuration.

```csharp
_pipelineBuilder
    .ExtractSqlite<SourceCustomer>(
        "Data Source=.\\source.db",
        "SELECT [CustomerId], [CustomerName], [Inactive] FROM [SourceCustomer]")
    .Transform<Customer>((source, mapper) => mapper.Apply(source))
    .LoadSqlite("Data Source=.\\target.db")
    .WithSlowlyChangingDimension()
```

Another benefit of this approach is that the api becomes more fluent. For example, if a loader is only applicable to an entity that inherits from a specific type, the load extension method can include the relevant constraints. The compiler will then complain when the loader extension method is used but not supported for the given type.

## Design

### Fluent Builder
Use of the fluent builder pattern is an intentional design choice. The alternative is to give the user apis for data extraction, transformation, and load, and let them decide how to connect everything together. Given the simplicity of a Rowbot data pipeline, enforcing correctness is preferable.

Pipeline builder is a set of interfaces and classes.

| Class | Interface | Description |
|---|---|---|
| `PipelineBuilder` | `IPipelineBuilder` | Collects pipeline level configuration and adds read connector |
| `PipelineExtractor<TSource>` | `IPipelineExtractor<TSource>`<br />`IPipelineTransformer<TPrevious, TSource>` | Adds extractor<br />Adds transformer |
| `PipelineTransformer<TPrevious, TSource>` | `IPipelineTransformer<TPrevious, TSource>` | Adds transformer or write connector |
| `PipelineLoader<TTarget>` | `IPipelineLoader<TTarget>` | Adds loader |

In the case of `PipelineExtractor<TSource>`, the api design is a little more complicated so as to improve the user experience. If the user doesn't specify an extractor, pipeline builder will automatically add an instance of  `DefaultExtractor<TSource>`. For this to work, `PipelineExtractor<TSource>` implements both `IPipelineExtractor<TSource>` and `IPipelineTransformer<TPrevious, TSource>` so that the fluent api can conditionally skip the add extractor step.

### Block Construction
`PipelineBuilderContext` passes state between pipeline builder classes as the pipeline is built. Each pipeline builder class adds one or more blocks to the context, plus any relevant metadata.

Because blocks are instantiated outside of dependency injection, they are not capable of constructor injection. Instead dependencies are passed directly to blocks from `PipelineBuilderContext`. Currently, the only dependencies required are `ILoggerFactory` and `ServiceFactory`.

### ServiceFactory
Pipeline builder is responsible for instantiating instances of connectors, extractors, transformers, and loaders. It does this using `ServiceFactory` which uses `IServiceProvider` under the hood to create instances of components registered for dependency injection.

| :information_source: Technical Note |
| --- |
| <p>Use of `IServiceProvider` is often associated with the service locator pattern, which is discouraged because it makes code more opaque by hiding dependencies. This leads to things like unit testing being more difficult.</p><p>Using `IServiceProvider` in Rowbot is justified because `ServiceFactory` is only responsible for instantiating instances of connectors, extractors, transformers, and loaders. For all of these components, service registration is the responsibility of the component maintainer and not of the user. The maintainer will register their component for dependency injection using an open generic, e.g. `ReadConnector<>` which removes the need for the user to explicitly register a service for each entity type, e.g. `ReadConnector<Customer>`. Therefore, the user does not need to know about the data type of the component and cannot fail to register a hidden dependency.</p><p>Additionally, unit testing a data pipeline in Rowbot is discouraged because unit testing involves mocking the underlying component (connector, extractor, transformer, loader). Testing the component is the responsibility of the maintainer, not the user. The user should only be concerned with unit testing the custom logic passed into the transformer and/or integration testing the data pipeline using actual data.</p><p>For these reasons, hiding a component's underlying service behind an extension method doesn't negatively affect the codebase as it otherwise might.</p> |

### Pipeline Definition
We refer to the code created using `IPipelineBuilder` as the pipeline definition. When this is invoked, it returns an object of type `Pipeline`. See [Contributor -> Pipeline](Pipeline.md)

### Customisation
To support extensibility via extension methods, pipeline builder allows external code to be plugged in.

The documentation for [Connectors](Connectors.md), [Extractors](Extractors.md), [Transformers](Transformers.md), and [Loaders](Loaders.md) provide examples of use.