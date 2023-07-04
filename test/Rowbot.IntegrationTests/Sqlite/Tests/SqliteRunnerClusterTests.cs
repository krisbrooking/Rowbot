using Rowbot.IntegrationTests.Setup.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace Rowbot.IntegrationTests.Sqlite.Tests
{
    [Collection("IntegrationTests")]
    public class SqliteRunnerClusterTests
    {
        public SqliteRunnerClusterTests()
        {
            SqliteTest.Reset();
        }

        private const int DELAY = 500;

        [Fact]
        public async Task PipelineClusters_Should_ExecuteConcurrently()
        {
            var pipelineContainers = new Type[] { typeof(Cluster1Pipelines), typeof(Cluster2Pipelines), typeof(Cluster3Pipelines), typeof(Cluster4Pipelines), typeof(Cluster5Pipelines) };

            var watch = new Stopwatch();
            watch.Start();
            await SqliteTest
                .BuildRunner(pipelineContainers)
                .RunAsync();
            watch.Stop();

            Assert.True(watch.ElapsedMilliseconds < pipelineContainers.Length * DELAY);
        }        

        private static Pipeline GetPipeline(IPipelineBuilder pipelineBuilder) =>
            pipelineBuilder
                .ExtractSqlite<SourceCustomer>(SqliteTest.ConnectionString, "SELECT [CustomerId], [CustomerName], [Inactive] FROM [SourceCustomer]")
                .IncludeOptions(options => options.AddParameters(async () =>
                {
                    await Task.Delay(DELAY);
                    return new List<ExtractParameterCollection> { new ExtractParameterCollection(new ExtractParameter("Parameter", typeof(int), 10)) };
                }))
                .Transform<Customer>((source, mapper) => mapper.Apply(source), mapper => Customer.ConfigureMapper(mapper))
                .LoadSqlite(SqliteTest.ConnectionString)
                .CopyRows();

        [Cluster("Cluster1")]
        public class Cluster1Pipelines : IPipelineContainer
        {
            private readonly IPipelineBuilder _pipelineBuilder;
            public Cluster1Pipelines(IPipelineBuilder pipelineBuilder) { _pipelineBuilder = pipelineBuilder; }
            public Pipeline Load() => GetPipeline(_pipelineBuilder);
        }

        [Cluster("Cluster2")]
        public class Cluster2Pipelines : IPipelineContainer
        {
            private readonly IPipelineBuilder _pipelineBuilder;
            public Cluster2Pipelines(IPipelineBuilder pipelineBuilder) { _pipelineBuilder = pipelineBuilder; }
            public Pipeline Load() => GetPipeline(_pipelineBuilder);
        }

        [Cluster("Cluster3")]
        public class Cluster3Pipelines : IPipelineContainer
        {
            private readonly IPipelineBuilder _pipelineBuilder;
            public Cluster3Pipelines(IPipelineBuilder pipelineBuilder) { _pipelineBuilder = pipelineBuilder; }
            public Pipeline Load() => GetPipeline(_pipelineBuilder);
        }

        [Cluster("Cluster4")]
        public class Cluster4Pipelines : IPipelineContainer
        {
            private readonly IPipelineBuilder _pipelineBuilder;
            public Cluster4Pipelines(IPipelineBuilder pipelineBuilder) { _pipelineBuilder = pipelineBuilder; }
            public Pipeline Load() => GetPipeline(_pipelineBuilder);
        }

        [Cluster("Cluster5")]
        public class Cluster5Pipelines : IPipelineContainer
        {
            private readonly IPipelineBuilder _pipelineBuilder;
            public Cluster5Pipelines(IPipelineBuilder pipelineBuilder) { _pipelineBuilder = pipelineBuilder; }
            public Pipeline Load() => GetPipeline(_pipelineBuilder);
        }
    }
}
