using Rowbot.Connectors.Sqlite;
using Rowbot.IntegrationTests.Setup;
using Rowbot.IntegrationTests.Setup.Entities;

namespace Rowbot.IntegrationTests.Tests
{
    [Collection("IntegrationTests")]
    public class PaginationTests
    {
        public PaginationTests()
        {
            SqliteTest.Reset();
        }

        [Fact]
        public async Task CursorPagination_Should_ExtractInPages()
        {
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(25));

            var result = await PipelineTest.RunPipelinesAsync<CustomerPipelines>()
                .RunAsync(pipelines => pipelines.FilterByTag("CursorPagination"));

            var rows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal(25, rows.Count());
            Assert.Equal(3, result.First(x => x.Name == "CursorPagination").BlockSummaries.First(x => x.Name == "PrimaryExtractBlock`2").TotalBatches);
        }

        [Fact]
        public async Task CursorPaginationZeroRows_Should_ExtractInPages()
        {
            await SqliteTest.WriteRowsAsync(Enumerable.Empty<SourceCustomer>());

            var result = await PipelineTest.RunPipelinesAsync<CustomerPipelines>()
                .RunAsync(pipelines => pipelines.FilterByTag("CursorPagination"));

            var rows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Empty(rows);
        }

        [Fact]
        public async Task OffsetPagination_Should_ExtractInPages()
        {
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(25));

            var result = await PipelineTest.RunPipelinesAsync<CustomerPipelines>()
                .RunAsync(pipelines => pipelines.FilterByTag("OffsetPagination"));

            var rows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal(25, rows.Count());
            Assert.Equal(3, result.First(x => x.Name == "OffsetPagination").BlockSummaries.First(x => x.Name == "PrimaryExtractBlock`2").TotalBatches);
        }

        [Fact]
        public async Task OffsetPaginationZeroRows_Should_ExtractInPages()
        {
            await SqliteTest.WriteRowsAsync(Enumerable.Empty<SourceCustomer>());

            var result = await PipelineTest.RunPipelinesAsync<CustomerPipelines>()
                .RunAsync(pipelines => pipelines.FilterByTag("OffsetPagination"));

            var rows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Empty(rows);
        }

        public class CustomerPipelines(IPipelineBuilder pipelineBuilder) : IPipeline
        {
            [Tag("CursorPagination")]
            public Pipeline CursorPagination() =>
                pipelineBuilder
                    .Extract<SourceCustomer>(builder => builder
                        .FromSqlite(
                            SqliteTest.ConnectionString,
                            "SELECT [CustomerId], [CustomerName], [Inactive] FROM [SourceCustomer] WHERE [CustomerId] > @CustomerId ORDER BY [CustomerId] LIMIT @BatchSize")
                        .WithCursorPagination(x => x.CustomerId),
                        options: new ExtractOptions(batchSize: 10))
                    .Transform(source => source.Select(x => new Customer(x.CustomerId, x.CustomerName, x.Inactive, x.Source)))
                    .Apply<Customer>(mapper => mapper
                        .Transform.ToHashCode(hash => hash.WithSeed(1).Include(x => x.Id), x => x.KeyHash)
                        .Transform.ToHashCode(hash => hash.WithSeed(1).All(), x => x.ChangeHash))
                    .Load(builder => builder
                        .ToSqlite(SqliteTest.ConnectionString));

            [Tag("OffsetPagination")]
            public Pipeline OffsetPagination() =>
                pipelineBuilder
                    .Extract<SourceCustomer>(builder => builder
                        .FromSqlite(
                            SqliteTest.ConnectionString,
                            "SELECT [CustomerId], [CustomerName], [Inactive] FROM [SourceCustomer] ORDER BY [CustomerId] LIMIT @BatchSize OFFSET @Offset")
                        .WithOffsetPagination(),
                        options: new ExtractOptions(batchSize: 10))
                    .Transform(source => source.Select(x => new Customer(x.CustomerId, x.CustomerName, x.Inactive, x.Source)))
                    .Apply<Customer>(mapper => mapper
                        .Transform.ToHashCode(hash => hash.WithSeed(1).Include(x => x.Id), x => x.KeyHash)
                        .Transform.ToHashCode(hash => hash.WithSeed(1).All(), x => x.ChangeHash))
                    .Load(builder => builder
                        .ToSqlite(SqliteTest.ConnectionString));
        }
    }
}
