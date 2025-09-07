# Rowbot

Rowbot is a data pipeline framework for the .NET developer. It provides a simple, fluent api to extract, transform, 
and load data. It includes a builder that ensures pipelines are authored correctly and consistently, and a runner 
that is responsible for executing them in the correct order.

## Why Rowbot?

Rowbot is an opinionated framework that intentionally trades flexibility for simplicity. Rowbot encourages the creation 
of many small pipelines; larger pipelines are built by combining smaller ones.

### Designed for developers

The fluent pipeline builder makes it quick and easy to move data from a source to a target system. With Rowbot, **pipelines
are code**, there is no graphical designer or reporting tool included. By default, the pipeline runner outputs to the console.

### Simple to extend

Rowbot is **designed to be extensible**. Custom components plug into the pipeline builder in exactly the same way as 
built-in components do. Extensions like custom data source connectors are simple to build and integrate into the api.

### Unit testable

Rowbot abstracts data extract and load so that the pipeline developer can focus on data transformation. Typically,
 **data transformation is a single C# method** with an input array and an output array, making the logic 
 easily testable.

## Get Started
A pipeline is created using `IPipelineBuilder`, a fluent builder that provides methods for extracting, transforming, 
and loading data.

```csharp
[Table("SourceCustomers")]
public sealed class SourceCustomer
{
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
}

[Table("TargetCustomers")]
public sealed class TargetCustomer
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

public class CustomerPipelines(IPipelineBuilder pipelineBuilder) : IPipeline
{
    public Pipeline Load() =>
        pipelineBuilder
            .Extract<SourceCustomer>(builder => builder
                .FromSqlite(
                    "Data Source=source.db",
                    "SELECT [CustomerId], [CustomerName] FROM [SourceCustomers]")
            )
            .Transform<Customer>(source => source
                .Select(customer => new Customer
                {
                    Id = customer.CustomerId, 
                    Name = customer.CustomerName
                })
            )
            .Load(builder => builder
                .ToSqlite("Data Source=target.db")
            );
}
```

This example uses the Sqlite read connector to extract data and the Sqlite write connector to load data. Rowbot is 
designed so that read and write connectors can be mixed and matched. By changing a few lines, the pipeline could 
extract data from an HTTP (JSON) endpoint and load into MS SQL Server.

### Create entities to represent source & target data
Two entities, `SourceCustomer` and `TargetCustomer` are defined. Entities describe the data format at the source and 
target systems.

```csharp
[Table("SourceCustomers")]
public sealed class SourceCustomer
{
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
}

[Table("TargetCustomers")]
public sealed class TargetCustomer
{
    public int Id { get; set; }
    public string? Name { get; set; }
}
```

### Create a pipeline container class
The pipeline container class `CustomerPipelines` implements the `IPipeline` marker interface which informs the 
framework that the class contains one or more pipelines.

```csharp
public class CustomerPipelines : IPipeline
```

### Author a pipeline with `IPipelineBuilder`
`IPipelineBuilder` is used to describe a pipeline that extracts customers from a Sqlite database **source.db**, 
transforms the source customers to another data type `TargetCustomer`, and loads entities of the `TargetCustomer` 
data type into a different Sqlite database **target.db**.

```csharp
public Pipeline Load() =>
    pipelineBuilder
        .Extract<SourceCustomer>(builder => builder
            .FromSqlite(
                "Data Source=source.db",
                "SELECT [CustomerId], [CustomerName] FROM [SourceCustomers]")
        )
        .Transform<Customer>(source => source
            .Select(customer => new Customer
            {
                Id = customer.CustomerId, 
                Name = customer.CustomerName
            })
        )
        .Load(builder => builder
            .ToSqlite("Data Source=target.db")
        );
```

## Run a Pipeline
Rowbot can be configured in a .NET Console project using `HostBuilder`. The `AddRowbot()` extension method registers 
required services. `AddRowbot()` will also scan for pipelines in the current assembly, or in a list of assemblies 
supplied as arguments. Any connectors that are used by a pipeline need to be registered for DI separately.

> Any class containing a data pipeline must implement the `IPipeline` marker interface so that it is scanned and pipelines are registered for execution.

To run data pipelines, get the instance of `IPipelineRunner`, and call `RunAsync()`

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rowbot;
using Rowbot.Connectors.Sqlite;

static async Task Main(string[] args)
{
    var host = Host.CreateDefaultBuilder(args)
        .ConfigureServices((services) =>
        {
            services.AddRowbot();
            services.AddSqliteConnector();
            services.AddConsoleSummary();
        })
        .Build();

    var pipelineRunner = host.Services.GetRequiredService<IPipelineRunner>();
    await pipelineRunner.RunAsync();
}
```

### Console Summary
`RunAsync` returns a list of `PipelineSummary` objects. Every pipeline that runs produces a `PipelineSummary` which 
includes success status, runtime, and other metadata about the execution.

By adding `AddConsoleSummary()` to the DI configuration of a .NET Console project, these pipeline summaries are 
formatted as a table and printed to the console after all pipelines have completed.

```
Pipelines completed: 1/1
Total Runtime:       00:01
________________________________________________________________________________________________________________________

Cluster|Group|Container                   |Name                                       |Status |Runtime |Inserts |Updates
------------------------------------------------------------------------------------------------------------------------
Default|1    |CustomerPipelines           |Load                                       |Success|   00:01|      10|      0
```
