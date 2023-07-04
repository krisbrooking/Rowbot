# Loaders

## Design
The loader manages the load phase of the ETL pipeline. It extends the functionality of the write connector to support common data warehouse load operations like loading facts and dimensions.

> The separation of loader and write connector allows many different connectors to load data using a single format.

A type 2 slowly changing dimension update is a good example of why the loader abstraction exists; it requires both an update and an insert operation to occur at the target. 

When a type 2 slowly changing dimension field is updated, the current row that represents the entity is marked as inactive, and a new entity with the updated field value is created. The original entity remains to keep a history of the old field value. The loader must instruct the write connector to update the existing entity and to create a new entity for a single type 2 update transaction.

### Inserting and Updating
The loader prepares an entity for insert or update before passing the entity to the respective method of the write connector.

- **Insert**: No preparation is necessary. The entity does not need to be modified for insert.
- **Update**: The entity must be wrapped in an `Update<TEntity>` object, where `TEntity` refers to the entity type being updated. The instance of `Update<TEntity>` must also include a list of changed fields. The `ChangedFields` proeprty is an `IEnumerable<FieldDescriptor>`; the write connector can use this to partially update the entity.

### Fault Tolerance
Loaders should be designed for fault tolerance - an exception that causes the data pipeline to crash should not result in corrupt data at the target system.

This can be achieved by creating idempotent operations or by using a mechanism like a transaction to provide a fallback in the event of an exception.

If we continue with the type 2 slowly changing dimension example, the built-in `SlowlyChangingDimensionLoader` implements both approaches for fault tolerance. 
- **Transactions**: For connectors that support ambient `TransactionScope`, that is implicit transactions where operations are wrapped in a using scope, the loader will only commit the update and insert operations if both succeed.
- **Idempotent Operations**: For connectors that don't support ambient transactions, the operations are processed in an order that reduces the likelihood of data corruption. First the update occurs, marking the existing entity as inactive, then the insertion of the new entity occurs. If the data pipeline were to exit before the insert operation, the loader will self-correct on the next run by only processing the insert operation. Although the data at the target system will not be correct until the data pipeline is rerun, the data is not corrupt.

```csharp
using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
{
    var rowsUpdated = await connector.UpdateAsync(type2RowsToUpdate);
    var rowsInserted = await connector.InsertAsync(type2RowsToInsert);

    scope.Complete();
}
```

See [src\Rowbot\Loaders\SlowlyChangingDimensions\SlowlyChangingDimensionLoader.cs](../../src/Rowbot/Loaders/SlowlyChangingDimensions/SlowlyChangingDimensionLoader.cs)

### Data Batching
Data is loaded in batches. The `LoadAsync()` method of `ILoader<TTarget>` accepts an array of entities of type `TTarget` as a parameter. Data is loaded in batches because this is typically the most efficient method of writing data. For the same reason, the write connector's `InsertAsync()` and `UpdateAsync()` methods accept an array of entities.

All pipelines have a default batch size of 1000.

## Extensibility - Create a Fact Loader
Custom loaders are designed to be simple to plug-in to the pipeline builder.

### 1. Create a loader options class

The options class for `FactLoader` requires no configurable state.
See [src\Rowbot\Loaders\Facts\FactLoaderOptions.cs](../../src/Rowbot/Loaders/Facts/FactLoaderOptions.cs)

`ILoader` requires a generic argument `TOptions`. This is a class that holds any user-configurable state required by the loader. A loader is required to include this options class even if there is no user-configurable state. The rationale behind this decision is that in most cases additional state is required and in cases where it is not, an empty class is only visible to the developer of the loader and not to the user.

| :information_source: Technical Note |
| --- |
| <p>There are two `ILoader` interfaces, one with and one without the `TOptions` generic argument. This is to allow components to invoke methods on the loader without needing to specify the `TOptions` class.</p><p>A loader will not function if created by implementing the `ILoader<TSource>` interface. The loader developer must implement the `ILoader<TSource, TOptions>` interface.</p> |

### 2. Create loader and implement `ILoader<TSource, TOptions>`

`LoadAsync` uses the connector's `FindAsync()` method to determine whether any entities have already been inserted at target. It then uses the connector's `InsertAsync()` method to insert any entities that don't already exist.
See [src\Rowbot\Loaders\Facts\FactLoader.cs](../../src/Rowbot/Loaders/Facts/FactLoader.cs)

#### Write Connector
`ILoader<TSource, TOptions>` includes a property `Connector` of type `IWriteConnector<TSource>` that must be implemented. `Connector` is not known at construction of the extractor; it is configured by the user using the pipeline builder. `Connector` should be assigned to `new NullWriteConnector<TSource>()` in the constructor.

`ServiceFactory` is responsible for assigning the options and connector to the loader at runtime.

### 3. Create extension method to plug-in to pipeline builder

`FactLoader` includes an extension method, `WithFact`, that calls `extractor.WithCustomLoader` to register the loader with the pipeline.
See [src\Rowbot\Loaders\Facts\FactLoaderExtensions.cs](../../src/Rowbot/Loaders/Facts/FactLoaderExtensions.cs)