# Unit Testing

Although the `IPipelineBuilder` interface is injected into the pipeline container class as a dependency, it is not intended to be used to unit test the data pipeline.

There are several reasons for this.

1. A pipeline typically requires component specific configuration that is not easily mocked.
2. A pipeline definition is complicated and dependencies are intentionally hidden behind extension methods.
3. Components like connectors, extractors, and loaders should have been unit tested by the component developer.

The only component of the pipeline definition that should be unit tested is the transformer. This can be achieved easily by moving the transform logic into its own method.

```csharp
public Pipeline Load() =>
    _pipelineBuilder
        .ExtractSqlite<SourceCustomer>(...)
        .Transform<Customer>(
            (source, mapping) => TransformCustomers(source, mapping),
            mapperConfiguration =>
            {
                mapperConfiguration.Transform.ToHashCode(hash => hash.Include(x => x.CustomerId), target => target.KeyHash);
                mapperConfiguration.Transform.ToHashCode(hash => hash.All(), target => target.ChangeHash);
            }
        .LoadSqlite(...)
        .WithSlowlyChangingDimension();

internal Customer[] TransformCustomers(SourceCustomer[] source, Mapping<SourceCustomer, Customer> mapping)
{
    return source
        .Select(x => mapper.Apply(new Customer
        {
            Id = $"CUST{x.CustomerId}",
            Name = x.CustomerName
        }))
        .ToArray()
}
```

The `TransformCustomers()` method can then be tested.

```csharp
public void TransformCustomers_Should_PrefixIdWithCUST()
{
    var source = new[1] SourceCustomer { new SourceCustomer(1, "XYZ Corp") };

    var result = new CustomerPipelines(PipelineBuilder.NullInstance)
        .TransformCustomers(source, new Mapper<SourceCustomer, Customer>());

    Assert.Equal("CUST1", result.First().Id);
}
```