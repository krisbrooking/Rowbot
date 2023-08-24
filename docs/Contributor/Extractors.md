# Extractors

## Background
The extractor manages the extract phase of the ETL pipeline. It extends the functionality of the read connector.

The extractor is the most recent addition to Rowbot. It was added partially for completeness; the load stage has the loader, the transform stage has the transformer, and the extract stage has the extractor. However, it was primarily added to try to make data extraction more generic.

> The separation of extractor and read connector allows many different connectors to utilise support for a single mechanism of extraction like pagination while the separation of extractor and extract parameter generator allows user-defined customisation of the extractor.

### Responsibility of the Read Connector
Rowbot processes data in batches to reduce memory usage. This architecture requires data pagination to support larger datasets. Pagination was originally included as configuration in the read connector but this came with some issues.

- A read connector could potentially support multiple methods of pagination, each with different configuration. 
- Different read connectors could potentially support the same method of pagination meaning code is duplicated.
- The read connector now has more than a single responsibility. It should ideally be responsible for executing a query, not managing and executing queries.

### Limitations of a Single Read Connector
At some point it also became apparent that certain data pipelines were not possible to create without external parameters. This is the result of recommending small data pipelines; data can only originate from a single source and no additional data can be merged into the pipeline after the extract stage.

Often data pipelines are built to merge data. This works well when all data is queried from the same source/read connector. For example, a SQL table could be merged/joined with a second SQL table in a single query. Unfortunately, there is a problem when data must be merged from different sources/read connectors. This should ideally be possible using a single data pipeline but in reality would require up to three.
- **Data pipeline 1**: Get data from REST endpoint A and load to SQL table A.
- **Data pipeline 2**: Get data from REST endpoint B and load to SQL table B.
- **Data pipeline 3**: Join SQL tables A and B and load to SQL table C.

This pattern remains the recommended approach to data pipeline design when both REST endpoints return a lot of data. However, if endpoint B is only returning 20 items that are used for some type of mapping, then this approach is difficult to justify. A better solution is to retrieve the 20 items from endpoint B outside the data pipeline and inject them as parameters into the extract stage. This results in a single data pipeline to achieve the same result. 
- **Data pipeline 1**: Get data from REST endpoint A, inject data from REST endpoint B and load to SQL table A.

## Design
The extractor attempts to resolve these two issues. Unfortunately, creating an abstraction to manage the read connector is tricky because there are an infinite number of ways to orchestrate data extraction.

### Comparison to Loader
It might seem that the extractor is the inverse of the loader. This is intentional in terms of api design but isn't true regarding operation. The loader is responsible for orchestrating data load which is a relatively simple task because writing data to a target system typically doesn't require any additional context.

In contrast, reading data from a source system often does require additional context. Here are a few examples.
- To query a database table in pages, we need to know the page size and offset or next cursor.
- To query a HATEOAS supported REST endpoint, we need to get the next page link from the current HTTP response.
- To GET data from an OAuth secured HTTP endpoint, we need an access token.

### Generalising Data Extraction
The context required for data extraction can get even more complicated. Given the example above with REST endpoints A and B, we might want to query endpoint A 20 times, one each for every result from endpoint B and it might be that every request to endpoint A requires a separate access token.

Whether or not to create a custom connector for these sort of scenarios is a design decision for the data pipeline developer. Rowbot offers the following generalised abstraction as an alternative.

| Component | Responsibility |
|---|---|
| **ExtractParameterGenerator** | Generates additional context for the extractor |
| **Extractor** | Invokes a read connector's `QueryAsync()` method passing in additional context |
| **Read Connector** | Responsible for querying data |

#### Providing Additional Context via Extract Parameters
The extractor uses extract parameters to modify a query. An extractor can generate and/or provide user-generated extract parameters.

Some extractors generate their own extract parameters. For example, cursor pagination requires keeping track of a cursor and passing it to the read connector to include in each query.

Extractors can also optionally forward user-generated extract parameters to their read connectors. Every extractor specifies an options class in its definition. This class must inherit from `ExtractorOptions` (`ExtractorOptions` can be used directly if no custom options are required). `ExtractorOptions` includes methods that allow a user to provide extract parameters to the extractor. `AddParameter()` accepts a single extract parameter, and `AddParameters()` accepts a factory that generates extract parameters. Factory methods are invoked by the extractor during pipeline execution.

See more [User -> Extractors](../../docs/User/Extractors.md)

Using extract parameters, the previous example might be implemented as follows.

| Component | Description |
|---|---|
| **ExtractParameterGenerator** | A custom generator parses the 20 items from endpoint B and for each, generates an access token by querying an authorisation endpoint. |
| **Extractor** | A HTTP GET extractor invokes the custom generator and then calls the read connector's `QueryAsync()` method for each of the 20 extract parameters returned. The extractor also inspects the HTTP response and if it finds a next link property, it performs pagination by adding this additional context in its next call to `QueryAsync()`. |
| **Read Connector** | A HTTP GET connector accepts extract parameters and configures the HTTP request. The read connector returns the raw HTTP response. |

#### Limitations of this Approach
- Extract parameters are key value pairs where the key is a string. This works when modifying SQL queries or HTTP requests because configuration is string based. It is not as helpful when a read operation is modified using a programmatic method like a LINQ query. In this case, extract parameters would need to be ignored in favour of a custom connector with query filtering as configuration.
- Because only one extractor can exist per pipeline, each extractor must accept user-defined extract parameters on top of providing its own functionality.

### Data Streaming
The extractor is designed for streaming because typically we don't know how much data will be pulled from the source system prior to pipeline execution.

An extractor implements `IExtractor<TSource, TOptions>` which includes an `Extract()` method that returns an `IAsyncEnumerable<TSource>`.

> There is no synchronous version of the `Extract()` method. The justification for this decision is that most connectors are asynchronous or have an asynchronous overload because most connectors are IO-bound. All connectors are assumed to be asynchronous for the same reason.

### Data Batching
The transformer and loader accept batched data. The extract block is therefore responsible for receiving streaming data from an extractor and batching that data before passing it on to the transform block. Batch size is configured using the `BatchSize` property of the `ExtractorOptions` class. Extractors can set the value of this property internally and/or can optionally allow the user to modify it.

```mermaid
graph TD;
    subgraph Loader Input = Batch
        batchData3(A, B)
        batchData4(C, D)
    end
    
    subgraph Transformer Input = Batch
        batchData1(A, B)
        batchData2(C, D)
    end

    subgraph Extractor Output = Stream
        streamingData1(A)
        streamingData2(B)
        streamingData3(C)
        streamingData4(D)
    end
```

Although the extractor returns streaming data, the read connector's `QueryAsync()` method, which the extractor calls, returns an array. This allows the user to decide how data should be pulled from source.

In many cases, querying data from source in batches is most efficient, but data can also be queried using a batch size of 1. In either scenario, the extractor streams data to the extract block which batches it before passing it to the transform block.

## Extensibility - Create an Offset Pagination Extractor
Custom extractors are designed to be simple to plug-in to the pipeline builder.

### 1. Create an extractor options class

The options class for `OffsetPaginationExtractor` includes a data pager factory `Func<IDataPager<TSource>>` property. The data pager calculates the extract parameters required to query the next page of data.
See [src\Rowbot\Extractors\OffsetPagination\OffsetPaginationExtractorOptions.cs](../../src/Rowbot/Extractors/OffsetPagination/OffsetPaginationExtractorOptions.cs)

`IExtractor` requires a generic argument `TOptions` that inherits from `ExtractorOptions`. This is a class that holds any user-configurable state required by the extractor.

| :information_source: Developer Note |
| --- |
| <p>There are two `IExtractor` interfaces, one with and one without the `TOptions` generic argument. This is to allow components to invoke methods on the extractor without needing to specify the `TOptions` class. </p><p>An extractor will not function if created by implementing the `IExtractor<TSource>` interface. The extractor developer must implement the `IExtractor<TSource, TOptions>` interface.</p> |

### 2. Create extractor and implement `IExtractor<TSource, TOptions>`

`Extract()` uses the data pager and calls the read connector's `QueryAsync()` method, passing in the extract parameters generated for the current page.
See [src\Rowbot\Extractors\OffsetPagination\OffsetPaginationExtractor.cs](../../src/Rowbot/Extractors/OffsetPagination/OffsetPaginationExtractor.cs)

#### Read Connector
`IExtractor<TSource, TOptions>` includes a property `Connector` of type `IReadConnector<TSource>` that must be implemented. `Connector` is not known at construction of the extractor; it is configured by the user using the pipeline builder. `Connector` should be assigned to `new NullReadConnector<TSource>()` in the constructor. 

`ServiceFactory` is responsible for assigning the options and connector to the extractor at runtime.

### 3. Create extension method to plug-in to pipeline builder

`OffsetPaginationExtractor` includes an extension method, `WithOffsetPagination`, that configures the `DataPagerFactory` property of the options class with an `OffsetDataPager<TSource>` and passes in any user-configurable state and then calls `extractor.WithCustomExtractor` to register the extractor with the pipeline.
See [src\Rowbot\Extractors\OffsetPagination\OffsetPaginationExtractorExtensions.cs](../../src/Rowbot/Extractors/OffsetPagination/OffsetPaginationExtractorExtensions.cs)