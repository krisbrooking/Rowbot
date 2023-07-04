# Integration Testing
Integration testing of a Rowbot data pipeline is recommended over unit testing, however, some setup is required before integration testing is possible.

## Suggested Approach

### Pipeline Configuration
All pipelines are tested without change, the only difference between running a pipeline in production and running it within an integration test should be the configuration provided.

Pipeline configuration should be passed into the pipeline container class using constructor injection so that it can set by the host builder during setup of Rowbot.

```csharp
public class CustomerPipelines : IPipelineContainer
{
    private readonly IPipelineBuilder _pipelineBuilder;
    private readonly CustomerOptions _options;

    public CustomerPipeline(
        IPipelineBuilder pipelineBuilder, 
        IOptions<CustomerOptions> options)
    {
        _pipelineBuilder = pipelineBuilder;
        _options = options.Value;
    }

    public Pipeline Load() =>
        _pipelineBuilder
            .ExtractSqlite<SourceCustomer>(
                _options.SourceConnectionString,
                "SELECT [CustomerId], [CustomerName] FROM [SourceCustomer]")
            .Transform<TargetCustomer>(
                (source, mapper) =>
                    source
                    .Select(x => new TargetCustomer
                    {
                        Id = x.CustomerId,
                        Name = x.CustomerName
                    })
                    .ToArray())
            .LoadSqlite(_options.TargetConnectionString)
            .CopyRows();
}
```

### Setup Test Host
Create a method that configures Rowbot using `HostBuilder`. This method accepts one or more pipeline container types that are to be executed.

```csharp
public static class IntegrationTests
{
    public static IPipelineRunner BuildRunner(params Type[] pipelineContainerTypes)
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureHostConfiguration(hostConfig =>
            {
                hostConfig.AddJsonFile("testConfig.json", false);
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddRowbot(pipelineContainerTypes);
                services.AddSqliteConnector();

                services.Configure<CustomerOptions>(options => Configuration.GetSection("Customers").Bind(options));
            })
            .Build();

        return host.Services.GetRequiredService<IPipelineRunner>();
    }
}
```

### Create a Test
Every test requires a new instance of `IPipelineRunner`. By passing a list of pipeline container types into `BuildRunner()`, the integration test only executes a subset of a project's total pipelines. 

One or more tags can be passed into `RunAsync()` to filter even further and only run a subset of pipelines within a pipeline container.

```csharp
public async Task LoadCustomers_Should_CopyRows()
{
    // Write any seed data to source
    await WriteRowsAsync(SourceCustomer.GetValidEntities(10));

    await IntegrationTests
        .BuildRunner(typeof(CustomerPipelines))
        .RunAsync();

    // Read data from target
    var rows = await ReadRowsAsync<Customer>();

    Assert.Equal(10, rows.Count());
}
```