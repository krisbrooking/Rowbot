# Loaders
A loader manages the load phase of a data pipeline. It extends the functionality of the write connector to support common data warehouse load operations like loading facts and dimensions.

To use a loader in a pipeline, include it directly after the write connector declaration.

| :information_source: Technical Note |
| --- |
| <p>Extension methods for loaders should always use <strong>With</strong> as a prefix. This differentiates them from write connectors which should use <strong>Load</strong> as a prefix.</p> |

Rowbot includes several built-in loaders.

## Row Loader
The row loader inserts rows using a write connector. It does not support updating.

```csharp
public Pipeline Load() =>
    _pipelineBuilder
        .ExtractSqlite<SourceCustomer>(...)
        .LoadSqlite(targetConnectionString)
        .CopyRows();
```

## Fact Loader
The fact loader inserts new rows using a write connector. It does not support updating.

The fact loader differs from the row loader in that it does not insert a row that already exists. To support this functionality, the entity being loaded must inherit from `Fact`. The `KeyHash` property is used to determine whether a row already exists.

```csharp
public Pipeline Load() =>
    _pipelineBuilder
        .ExtractSqlite<SourceCustomer>(...)
        .Transform<Customer>(
            (source, mapper) =>
                source
                .Select(x => mapper.Apply(new Customer
                {
                    CustomerId = x.Id,
                    CustomerName = x.Name
                }))
                .ToArray(),
            mapperConfiguration =>
            {
                mapperConfiguration.Transform.ToHashCode(hash => hash.Include(x => x.CustomerId), target => target.KeyHash);
            })
        .LoadSqlite(targetConnectionString)
        .WithFact();
```

## Snapshot Fact Loader
The snapshot fact loader inserts and updates rows using a write connector.

The snapshot fact loader differs from the fact loader in that it supports updating existing rows. To support this functionality, the entity being loaded must inherit from `Fact`. The `KeyHash` property is used to determine whether a row already exists. The `ChangeHash` property is used to determine whether any value of a row has changed.

```csharp
public Pipeline Load() =>
    _pipelineBuilder
        .ExtractSqlite<SourceCustomer>(...)
        .Transform<Customer>(
            (source, mapper) =>
                source
                .Select(x => mapper.Apply(new Customer
                {
                    CustomerId = x.Id,
                    CustomerName = x.Name
                }))
                .ToArray(),
            mapperConfiguration =>
            {
                mapperConfiguration.Transform.ToHashCode(hash => hash.Include(x => x.CustomerId), target => target.KeyHash);
                mapperConfiguration.Transform.ToHashCode(hash => hash.All(), target => target.ChangeHash);
            })
        .LoadSqlite(targetConnectionString)
        .WithSnapshotFact();
```

## Slowly Changing Dimension Loader
The slowly changing dimension loader inserts and updates rows using a write connector.

The slowly changing dimension loader differs from the snapshot fact loader in that it supports updating existing rows with change history. To support this functionality, the entity being loaded must inherit from `Dimension`. The `KeyHash` property is used to determine whether a row already exists. The `ChangeHash` property is used to determine whether any value of a row has changed.

```csharp
public Pipeline Load() =>
    _pipelineBuilder
        .ExtractSqlite<SourceCustomer>(...)
        .Transform<Customer>(
            (source, mapper) =>
                source
                .Select(x => mapper.Apply(new Customer
                {
                    CustomerId = x.Id,
                    CustomerName = x.Name
                }))
                .ToArray(),
            mapperConfiguration =>
            {
                mapperConfiguration.Transform.ToHashCode(hash => hash.Include(x => x.CustomerId), target => target.KeyHash);
                mapperConfiguration.Transform.ToHashCode(hash => hash.All(), target => target.ChangeHash);
            })
        .LoadSqlite(targetConnectionString)
        .WithSlowlyChangingDimension();
```