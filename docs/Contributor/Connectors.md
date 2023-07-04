# Connectors

## Background
Although connectors were one of the original features of Rowbot, the implementation has evolved. Connectors began as the only inputs to the extract and load stage of a data pipeline. They were injected via the constructor and provided as parameters to the `Extract` and `Load` methods of the pipeline builder.

```csharp
_pipelineBuilder
    .Extract(_sourceConnector)
    .Transform((source, mapper) => Transform(source, mapper))
    .Load(_targetConnector);
```

### Dependency Injection
Over time it became apparent that making every connector a dependency was problematic.

A core design goal of Rowbot is to support many pipelines in a single class. With dependency injection, we need both a read and a write connector injected into the constructor for each pipeline. This means the class constructor and fields become more and more unwieldy as pipelines are added. More importantly, if multiple pipelines happen to read from or write to the same entity, they would have to share an instance of the connector.

Testing a data pipeline was also difficult to justify in this context. Testing the extract and load stages of a data pipeline is only useful when real data is being processed. Therefore, if connectors aren't being tested, then we don't need pipeline specific interfaces, which means we can do away with explicit dependency injection.

After these early experiments, the options class, extension methods, and the `ServiceFactory` were introduced as an alternative.

| :information_source: Technical Note |
| --- |
| <p>Connectors are still registered for dependency injection so that they can access their own dependencies.</p><p>The connector type is registered in DI as an open generic and the `ServiceFactory` is responsible for instantiating the object. Because `ServiceFactory` relies on `IServiceProvider`, unit testing a data pipeline becomes complicated. This complication is compounded when the `ServiceFactory` is also creating an extractor, transformer, and loader.</p> |

## Design
Connectors are responsible for reading data from a source system or writing data to a target system. These responsibilities are split between the two ends of the data pipeline and are not necessarily supported for every system. For these reasons, read and write operations are split between interfaces.

- `IReadConnector` supports querying data.
- `IWriteConnector` supports inserting, updating, and finding data.
- `ISchemaConnector` supports creating a data store.

### Operations

| Operation | Interface | Description |
|---|---|---|
| **Query** | IReadConnector | `QueryAsync()` is the only method required by `IReadConnector`. It is designed to query batches of data because this is typically the most efficient way of pulling data from a source system. The batch size can be modified where appropriate.<br><br>`QueryAsync()` accepts an `IEnumerable<ExtractParameter>` as its only parameter. This allows the extractor to modify the query operation in cases where the connector supports query parameters. |
| **Find** | IWriteConnector | Although find is a read operation, it is included on the `IWriteConnector` interface because the write connector is concerned with loading rather than simply writing data. In many data load scenarios, we want to know if a row exists on the target system before attempting to insert or update.<br><br>If the find operation was included in `IReadConnector`, then all write-only connectors would have to implement `IReadConnector` including the `QueryAsync()` method. At the same time, all read-only connectors would have to implement the `Find()` method which would be unused. |
| **Insert** | IWriteConnector | Inserting an entity is an operation common to all write connectors and so is its own method. |
| **Update** | IWriteConnector | Updating an entity is not necessarily common to all write connectors but is important for most data load scenarios. In cases where a connector doesn't support updates, this method could return an empty response `return Task.FromResult(0);` or throw an exception to notify the user that loaders that update entities are not compatible.<br><br>Deletes can potentially be achieved by the connector through the `UpdateAsync()` method but soft deletes (where a row is marked as deleted) are preferred which is why a separate Delete operation is not included in `IWriteConnector`. |
| **Create DataSet** | ISchemaConnector | In many cases, a connector should be able to create a data set (file, SQL table, etc) prior to data load. This is an optional operation and is often appended to the write connector. |

## Extensibility - Create a Sqlite Connector
Custom connectors are designed to be simple to plug-in to the pipeline builder.

| :information_source: Rowbot Convention |
| --- |
| <p>A connector should include two connector classes.</p><p>A <strong>Read</strong> connector class that implements `IReadConnector<TSource, TOptions>` and/or a <strong>Write</strong> connector class that implements `IWriteConnector<TTarget, TOptions>`.</p> |


### 1. Create a connector options class or classes
See [src\Rowbot.Connectors.Sqlite\SqliteConnectorOptions.cs](../../src/Rowbot.Connectors.Sqlite/SqliteConnectorOptions.cs)

`IReadConnector` and `IWriteConnector` interfaces require a generic argument `TOptions`. This is a class that holds any user-configurable state required by the connector. A connector is required to include this options class even if there is no user-configurable state. The rationale behind this decision is that in most cases additional state is required and in cases where it is not, an empty class is only visible to the developer of the connector and not to the user.

| :information_source: Developer Note |
| --- |
| <p>`IReadConnector` and `IWriteConnector` include two interfaces each, one with and one without the `TOptions` generic argument. This is to allow other components like the extractor and loader to invoke methods on the interface without needing to specify the `TOptions` class.</p><p>A connector will not function if created by implementing the `IReadConnector<TSource>` or `IWriteConnector<TTarget>` interface. The connector developer must implement the `IReadConnector<TSource, TOptions>` or `IWriteConnector<TTarget, TOptions>` interface or both.</p> |


### 2. Create read connector and implement `IReadConnector<TSource, TOptions>`
See [src\Rowbot.Connectors.Sqlite\SqliteReadConnector.cs](../../src/Rowbot.Connectors.Sqlite/SqliteReadConnector.cs)

#### Description
##### `QueryAsync`
- `EntityDescriptor` and `EntityAccessor` are used to generically map values into the Rowbot entity.
- `SqliteCommandProvider` is used to convert between the Sqlite data type and the Rowbot entity data type.

##### `GetQueryCommands`
- `GetQueryCommands` generates a `SqliteCommand` and injects extract parameters.


### 3. Create write connector and implement `IWriteConnector<TSource, TOptions>`
See [src\Rowbot.Connectors.Sqlite\SqliteWriteConnector.cs](../../src/Rowbot.Connectors.Sqlite/SqliteWriteConnector.cs)

#### Description
##### `FindAsync`
- `SqliteCommandProvider` is used to generate find commands given a collection of fields to compare and a collection of fields to return.
- `EntityDescriptor` and `EntityAccessor` are used to generically map values into the Rowbot entity.
- `SqliteCommandProvider` is used to convert between the Sqlite data type and the Rowbot entity data type.

##### `InsertAsync` and `UpdateAsync`
- `SqliteCommandProvider` is used to generate insert/update commands


### 4. Create extension methods to plug-in to pipeline builder
See [src\Rowbot.Connectors.Sqlite\Extensions\SqliteConnectorExtensions.cs](../../src/Rowbot.Connectors.Sqlite/Extensions/SqliteConnectorExtensions.cs)

#### Description
##### `ExtractSqlite`
- Calls `pipelineBuilder.Extract()` to register the read connector with the configured extractor.

##### `LoadSqlite`
- Calls `pipelineBuilder.Load()` to register the write connector with the configured loader.


### 5. Create extension method to register connector with dependency injection
See [src\Rowbot.Connectors.Sqlite\DependencyInjection\SqliteInstaller.cs](../../src/Rowbot.Connectors.Sqlite/DependencyInjection/SqliteInstaller.cs)

- `SqliteReadConnector<>` and `SqliteWriteConnector<>` must be registered for dependency injection. Note: these are the base interfaces that do not accept the generic argument `TOptions`.