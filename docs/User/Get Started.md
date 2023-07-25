# User - Get Started

## Build a Pipeline
A pipeline is created using `IPipelineBuilder`, a fluent builder that provides methods for extracting, transforming, and loading data. 

The following example demonstrates the most common elements of a Rowbot data pipeline.

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

public class CustomerPipelines : IPipelineContainer
{
    private readonly IPipelineBuilder _pipelineBuilder;

    public CustomerPipelines(IPipelineBuilder pipelineBuilder) { _pipelineBuilder = pipelineBuilder; }

    public Pipeline Load() =>
        _pipelineBuilder
            .ExtractSqlite<SourceCustomer>(
                "Data Source=source.db",
                "SELECT [CustomerId], [CustomerName] FROM [SourceCustomers]")
            .Transform<TargetCustomer>(
                (source, mapper) =>
                    source
                    .Select(x => new TargetCustomer
                    {
                        Id = x.CustomerId,
                        Name = x.CustomerName
                    })
                    .ToArray())
            .LoadSqlite("Data Source=target.db")
            .CopyRows();
}
```

### Description

#### Create Entities to Represent Source & Target Data
Two entities, `SourceCustomer` and `TargetCustomer` are defined. Entities describe the data format at the source and target systems.

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

#### Create a Pipeline Container Class
The pipeline container class `CustomerPipelines` implements the `IPipelineContainer` marker interface which informs the framework that the class contains one or more pipelines.

```csharp
public class CustomerPipelines : IPipelineContainer
```

#### Author a Pipeline With `IPipelineBuilder`
`IPipelineBuilder` is used to describe a pipeline that extracts customers from a Sqlite database **source.db**, transforms the source customers to another data type `TargetCustomer`, and loads entities of the `TargetCustomer` data type into a different Sqlite database **target.db**.

This pipeline uses the Sqlite read connector to extract data from a Sqlite database and the Sqlite write connector to load data to a Sqlite database. Rowbot is designed so that read and write connectors can be mixed and matched. Data could just as easily be extracted from a CSV file and loaded into a Microsoft SQL Server database.

| :information_source: Rowbot Convention |
| --- |
| <p>Extension methods for read connectors should always use <strong>Extract</strong> as a prefix. Extension methods for write connectors should always use <strong>Load</strong> as a prefix.</p> |

```csharp
public CustomerPipelines(IPipelineBuilder pipelineBuilder) { _pipelineBuilder = pipelineBuilder; }

public Pipeline Load() =>
    _pipelineBuilder
        .ExtractSqlite<SourceCustomer>(
            "Data Source=source.db",
            "SELECT [CustomerId], [CustomerName] FROM [SourceCustomers]")
        .Transform<TargetCustomer>(
            (source, mapper) =>
                source
                .Select(x => new TargetCustomer
                {
                    Id = x.CustomerId,
                    Name = x.CustomerName
                })
                .ToArray())
        .LoadSqlite("Data Source=target.db")
        .CopyRows();
```

## Run a Pipeline
Rowbot can be configured in a .NET Console project using `HostBuilder`. The `AddRowbot()` extension method is used to register required services with dependency injection. `AddRowbot()` will also scan for pipelines in the current assembly, or in a list of assemblies supplied as arguments. Any connectors that are used by a pipeline need to be registered for DI separately.

> Any class containing a data pipeline must implement the `IPipelineContainer` marker interface so that it is scanned and pipelines are registered for execution.

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
`RunAsync` returns a list of `PipelineSummary` objects. Every pipeline that runs produces a `PipelineSummary` which includes success status, runtime, and other metadata about the execution. 

By adding `AddConsoleSummary()` to the DI configuration of a .NET Console project, these pipeline summaries are formatted as a table and printed to the console after all pipelines have completed.

```
Pipelines completed: 1/1
Total Runtime:       00:01
________________________________________________________________________________________________________________________

Cluster|Group|Container                   |Name                                       |Status |Runtime |Inserts |Updates
------------------------------------------------------------------------------------------------------------------------
Default|1    |CustomerPipelines           |Load                                       |Success|   00:01|      14|      3
```

## Learn More
### Built-in Components
Rowbot includes built-in components that extend the base functionality.

A connector provides access to read from a source, or write to a target system. Rowbot includes pre-built connectors for Sqlite and Microsoft SQL Server.

An extractor provides additional context to a read connector in order to provide support for read operations that span multiple requests, like pagination.<br />
[User -> Extractors](Extractors.md)

A loader manages the load phase of a data pipeline. It extends the functionality of the write connector to support common data warehouse load operations.<br />
[User -> Loaders](Loaders.md)

### Suggested Structure
Rowbot is an opinionated framework and recommends a specific approach to pipeline, class, and project structure.

[User -> Pipeline Structure](Pipeline%20Structure.md)

[User -> Class Structure](Class%20Structure.md)

[User -> Project Structure](Project%20Structure.md)

### Testing
Rowbot includes support for unit and integration testing.

[User -> Unit Testing](Unit%20Testing.md)

[User -> Integration Testing](Integration%20Testing.md)

## Full Code Example
Configuring Rowbot in a new project requires cloning the Rowbot repository and then creating a console project that references Rowbot.

```console
mkdir RowbotExample && cd RowbotExample

git clone https://github.com/krisbrooking/Rowbot

mkdir Example && cd Example

dotnet new console
dotnet add Example.csproj reference ../Rowbot/src/Rowbot/Rowbot.csproj
dotnet add Example.csproj reference ../Rowbot/src/Rowbot.Connectors.Sqlite/Rowbot.Connectors.Sqlite.csproj
dotnet add Example.csproj package Microsoft.Extensions.Hosting
dotnet add Example.csproj package Microsoft.Extensions.DependencyInjection
dotnet add Example.csproj package Microsoft.Data.Sqlite
dotnet build && dotnet run
```

Program.cs
```csharp
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rowbot;
using Rowbot.Connectors.Sqlite;
using System.ComponentModel.DataAnnotations.Schema;

namespace Example
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            InitialiseDatabase();

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

        static void InitialiseDatabase()
        {
            using (var db = new SqliteConnection($"Data Source=source.db"))
            {
                db.Open();

                var createTableCommand = 
                    new SqliteCommand(@"
                        DROP TABLE IF EXISTS SourceCustomers;
                        CREATE TABLE SourceCustomers (
                            CustomerId INTEGER PRIMARY KEY,
                            CustomerName NVARCHAR(100) NULL)", db);
                createTableCommand.ExecuteReader();

                var insertCommand = 
                    new SqliteCommand(string.Join(';', Enumerable.Range(0, 100).Select(x => $"INSERT INTO SourceCustomers VALUES ({x}, 'Cust{x}')")), db);
                insertCommand.ExecuteReader();
            }
        }
    }

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

    public class CustomerPipelines : IPipelineContainer
    {
        private readonly IPipelineBuilder _pipelineBuilder;

        public CustomerPipelines(IPipelineBuilder pipelineBuilder) { _pipelineBuilder = pipelineBuilder; }

        public Pipeline Load() =>
            _pipelineBuilder
                .ExtractSqlite<SourceCustomer>(
                    "Data Source=source.db",
                    "SELECT [CustomerId], [CustomerName] FROM [SourceCustomers]")
                .Transform<TargetCustomer>(
                    (source, mapper) =>
                        source
                        .Select(x => new TargetCustomer
                        {
                            Id = x.CustomerId,
                            Name = x.CustomerName
                        })
                        .ToArray())
                .LoadSqlite("Data Source=target.db")
                .CopyRows();
    }
}
```