using Rowbot.IntegrationTests.Setup.Entities;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Rowbot.IntegrationTests.Sqlite.Tests
{
    [Collection("IntegrationTests")]
    public class SqlitePaginationTests
    {
        public SqlitePaginationTests()
        {
            SqliteTest.Reset();
        }

        [Fact]
        public async Task CursorPagination_Should_ExtractInPages()
        {
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(25));

            var result = await SqliteTest
                .BuildRunner(typeof(CustomerPipelines))
                .RunAsync(pipelines => pipelines.FilterByTag("CursorPagination"));

            var rows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal(25, rows.Count());
            Assert.Equal(3, result.First(x => x.Name == "CursorPagination").BlockSummaries.First(x => x.Name == "ExtractBlock`1").TotalBatches);
        }

        [Fact]
        public async Task CursorPaginationZeroRows_Should_ExtractInPages()
        {
            await SqliteTest.WriteRowsAsync(Enumerable.Empty<SourceCustomer>());

            await SqliteTest
                .BuildRunner(typeof(CustomerPipelines))
                .RunAsync(pipelines => pipelines.FilterByTag("CursorPagination"));

            var rows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Empty(rows);
        }

        [Fact]
        public async Task OffsetPagination_Should_ExtractInPages()
        {
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(25));

            var result = await SqliteTest
                .BuildRunner(typeof(CustomerPipelines))
                .RunAsync(pipelines => pipelines.FilterByTag("OffsetPagination"));

            var rows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal(25, rows.Count());
            Assert.Equal(3, result.First(x => x.Name == "OffsetPagination").BlockSummaries.First(x => x.Name == "ExtractBlock`1").TotalBatches);
        }

        [Fact]
        public async Task OffsetPaginationZeroRows_Should_ExtractInPages()
        {
            await SqliteTest.WriteRowsAsync(Enumerable.Empty<SourceCustomer>());

            await SqliteTest
                .BuildRunner(typeof(CustomerPipelines))
                .RunAsync(pipelines => pipelines.FilterByTag("OffsetPagination"));

            var rows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Empty(rows);
        }

        public class CustomerPipelines : IPipelineContainer
        {
            private readonly IPipelineBuilder _pipelineBuilder;

            public CustomerPipelines(IPipelineBuilder pipelineBuilder)
            {
                _pipelineBuilder = pipelineBuilder;
            }

            [Tag("CursorPagination")]
            public Pipeline CursorPagination() =>
                _pipelineBuilder
                    .ExtractSqlite<SourceCustomer>(
                        SqliteTest.ConnectionString,
                        "SELECT [CustomerId], [CustomerName], [Inactive] FROM [SourceCustomer] WHERE [CustomerId] > @CustomerId ORDER BY [CustomerId] LIMIT @BatchSize")
                    .IncludeOptions(options => options.BatchSize = 10)
                    .WithCursorPagination(x => x.CustomerId)
                    .Transform<Customer>((source, mapper) => mapper.Apply(source), mapper => Customer.ConfigureMapper(mapper))
                    .LoadSqlite(SqliteTest.ConnectionString)
                    .CopyRows();

            [Tag("OffsetPagination")]
            public Pipeline OffsetPagination() =>
                _pipelineBuilder
                    .ExtractSqlite<SourceCustomer>(
                        SqliteTest.ConnectionString,
                        "SELECT [CustomerId], [CustomerName], [Inactive] FROM [SourceCustomer] ORDER BY [CustomerId] LIMIT @BatchSize OFFSET @Offset")
                    .IncludeOptions(options => options.BatchSize = 10)
                    .WithOffsetPagination()
                    .Transform<Customer>((source, mapper) => mapper.Apply(source), mapper => Customer.ConfigureMapper(mapper))
                    .LoadSqlite(SqliteTest.ConnectionString)
                    .CopyRows();
        }
    }
}
