using Rowbot.IntegrationTests.Setup.Entities;
using System.Diagnostics;
using Rowbot.Connectors.Sqlite;
using Rowbot.IntegrationTests.Setup;

namespace Rowbot.IntegrationTests.Tests
{
    [Collection("IntegrationTests")]
    public class RunnerClusterTests
    {
        public RunnerClusterTests()
        {
            SqliteTest.Reset();
        }

        private const int DELAY = 500;

        [Fact]
        public async Task PipelineClusters_Should_ExecuteConcurrently()
        {
            await SqliteTest.WriteRowsAsync(Enumerable.Empty<SourceCustomer>());

            var pipelineContainers = new Type[] { typeof(Cluster1Pipelines), typeof(Cluster2Pipelines), typeof(Cluster3Pipelines), typeof(Cluster4Pipelines), typeof(Cluster5Pipelines) };

            var watch = new Stopwatch();
            watch.Start();
            await PipelineTest.RunPipelinesAsync(pipelineContainers).RunAsync();
            watch.Stop();

            Assert.True(watch.ElapsedMilliseconds < pipelineContainers.Length * DELAY);
        }

        private static Pipeline GetPipeline(IPipelineBuilder pipelineBuilder) =>
            pipelineBuilder
                .Extract<SourceCustomer>(builder => builder
                    .FromSqlite(SqliteTest.ConnectionString, "SELECT [CustomerId], [CustomerName], [Inactive] FROM [SourceCustomer]"),
                    options: new ExtractOptions(batchSize: 10))
                .Transform(async source =>
                {
                    await Task.Delay(DELAY);
                    return source;
                })
                .Transform(source => source.Select(x => new Customer(x.CustomerId, x.CustomerName, x.Inactive, x.Source)))
                .Apply<Customer>(mapper => mapper
                    .Transform.ToHashCode(hash => hash.WithSeed(1).Include(x => x.Id), x => x.KeyHash)
                    .Transform.ToHashCode(hash => hash.WithSeed(1).All(), x => x.ChangeHash))
                .Load(builder => builder
                    .ToSqlite(SqliteTest.ConnectionString));

        [Cluster("Cluster1")]
        public class Cluster1Pipelines(IPipelineBuilder pipelineBuilder) : IPipeline
        {
            public Pipeline Load() => GetPipeline(pipelineBuilder);
        }

        [Cluster("Cluster2")]
        public class Cluster2Pipelines(IPipelineBuilder pipelineBuilder) : IPipeline
        {
            public Pipeline Load() => GetPipeline(pipelineBuilder);
        }

        [Cluster("Cluster3")]
        public class Cluster3Pipelines(IPipelineBuilder pipelineBuilder) : IPipeline
        {
            public Pipeline Load() => GetPipeline(pipelineBuilder);
        }

        [Cluster("Cluster4")]
        public class Cluster4Pipelines(IPipelineBuilder pipelineBuilder) : IPipeline
        {
            public Pipeline Load() => GetPipeline(pipelineBuilder);
        }

        [Cluster("Cluster5")]
        public class Cluster5Pipelines(IPipelineBuilder pipelineBuilder) : IPipeline
        {
            public Pipeline Load() => GetPipeline(pipelineBuilder);
        }
    }
}
