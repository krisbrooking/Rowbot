using Rowbot.IntegrationTests.Setup.Entities;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Rowbot.IntegrationTests.Sqlite.Tests
{
    [Collection("IntegrationTests")]
    public class SqliteExtractParameterTests
    {
        public SqliteExtractParameterTests()
        {
            SqliteTest.Reset();
        }

        [Fact]
        public async Task DefaultExtractor_Should_PassThroughExtractParameters()
        {
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(10));

            await SqliteTest
                .BuildRunner(typeof(CustomerPipelines))
                .RunAsync(pipelines => pipelines.FilterByTag("Parameters"));

            var rows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal(10, rows.Count());
            Assert.Equal(20, rows.Last().Id);
        }

        [Fact]
        public async Task DefaultExtractor_Should_PassThroughExtractParametersWithFactory()
        {
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(10));

            await SqliteTest
                .BuildRunner(typeof(CustomerPipelines))
                .RunAsync(pipelines => pipelines.FilterByTag("Factory"));

            var rows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal(50, rows.Count());
            Assert.Equal(50, rows.Last().Id);
        }

        [Fact]
        public async Task DefaultExtractor_Should_PassThroughExtractParametersWithAsyncFactory()
        {
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(10));

            await SqliteTest
                .BuildRunner(typeof(CustomerPipelines))
                .RunAsync(pipelines => pipelines.FilterByTag("AsyncFactory"));

            var rows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal(50, rows.Count());
            Assert.Equal(50, rows.Last().Id);
        }

        [Fact]
        public async Task CursorPaginationExtractor_Should_ExtractInPagesAndPassThroughExtractParameters()
        {
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(25));

            var result = await SqliteTest
                .BuildRunner(typeof(CustomerPipelines))
                .RunAsync(pipelines => pipelines.FilterByTag("CursorPagination"));

            var rows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal(125, rows.Count());
            Assert.True(rows.Last().Name?.EndsWith("40"));
            Assert.Equal(13, result.First(x => x.Name == "CursorPagination").BlockSummaries.First(x => x.Name == "ExtractBlock`1").TotalBatches);
        }

        [Fact]
        public async Task OffsetPaginationExtractor_Should_ExtractInPagesAndPassThroughExtractParameters()
        {
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(25));

            var result = await SqliteTest
                .BuildRunner(typeof(CustomerPipelines))
                .RunAsync(pipelines => pipelines.FilterByTag("OffsetPagination"));

            var rows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal(125, rows.Count());
            Assert.True(rows.Last().Name?.EndsWith("40"));
            Assert.Equal(13, result.First(x => x.Name == "OffsetPagination").BlockSummaries.First(x => x.Name == "ExtractBlock`1").TotalBatches);
        }

        public class CustomerPipelines : IPipelineContainer
        {
            private readonly IPipelineBuilder _pipelineBuilder;

            public CustomerPipelines(IPipelineBuilder pipelineBuilder)
            {
                _pipelineBuilder = pipelineBuilder;
            }

            [Tag("Parameters")]
            public Pipeline Parameters() =>
                _pipelineBuilder
                    .ExtractSqlite<SourceCustomer>(
                        SqliteTest.ConnectionString,
                        "SELECT [CustomerId] + @Parameter AS [CustomerId], [CustomerName], [Inactive] FROM [SourceCustomer]")
                    .IncludeOptions(options => options.AddParameters(new ExtractParameterCollection(new ExtractParameter($"Parameter", typeof(int), 10))))
                    .Transform<Customer>((source, mapper) => mapper.Apply(source), mapper => Customer.ConfigureMapper(mapper))
                    .LoadSqlite(SqliteTest.ConnectionString)
                    .CopyRows();

            [Tag("Factory")]
            public Pipeline Factory() =>
                _pipelineBuilder
                    .ExtractSqlite<SourceCustomer>(
                        SqliteTest.ConnectionString,
                        "SELECT [CustomerId] + @Parameter AS [CustomerId], [CustomerName], [Inactive] FROM [SourceCustomer]")
                    .IncludeOptions(options => options.AddParameters(() => 
                        Enumerable
                            .Range(0, 5)
                            .Select(x => new ExtractParameterCollection(new ExtractParameter($"Parameter", typeof(int), x * 10)))
                            .ToList()))
                    .Transform<Customer>((source, mapper) => mapper.Apply(source), mapper => Customer.ConfigureMapper(mapper))
                    .LoadSqlite(SqliteTest.ConnectionString)
                    .CopyRows();

            [Tag("AsyncFactory")]
            public Pipeline AsyncFactory() =>
                _pipelineBuilder
                    .ExtractSqlite<SourceCustomer>(
                        SqliteTest.ConnectionString,
                        "SELECT [CustomerId] + @Parameter AS [CustomerId], [CustomerName], [Inactive] FROM [SourceCustomer]")
                    .IncludeOptions(options => options.AddParameters(async () => 
                        await Task.FromResult(
                            Enumerable
                                .Range(0, 5)
                                .Select(x => new ExtractParameterCollection(new ExtractParameter($"Parameter", typeof(int), x * 10)))
                                .ToList())))
                    .Transform<Customer>((source, mapper) => mapper.Apply(source), mapper => Customer.ConfigureMapper(mapper))
                    .LoadSqlite(SqliteTest.ConnectionString)
                    .CopyRows();

            [Tag("CursorPagination")]
            public Pipeline CursorPagination() =>
                _pipelineBuilder
                    .ExtractSqlite<SourceCustomer>(
                        SqliteTest.ConnectionString,
                        "SELECT [CustomerId], [CustomerName] || @Parameter AS [CustomerName], [Inactive] FROM [SourceCustomer] WHERE [CustomerId] > @CustomerId ORDER BY [CustomerId] LIMIT @BatchSize")
                    .IncludeOptions(options =>
                    {
                        options.BatchSize = 10;
                        options.AddParameters(() =>
                            Enumerable
                                .Range(0, 5)
                                .Select(x => new ExtractParameterCollection(new ExtractParameter($"Parameter", typeof(int), x * 10)))
                                .ToList());
                    })
                    .WithCursorPagination(x => x.CustomerId)
                    .Transform<Customer>((source, mapper) => mapper.Apply(source), mapper => Customer.ConfigureMapper(mapper))
                    .LoadSqlite(SqliteTest.ConnectionString)
                    .CopyRows();

            [Tag("OffsetPagination")]
            public Pipeline OffsetPagination() =>
                _pipelineBuilder
                    .ExtractSqlite<SourceCustomer>(
                        SqliteTest.ConnectionString,
                        "SELECT [CustomerId], [CustomerName] || @Parameter AS [CustomerName], [Inactive] FROM [SourceCustomer] ORDER BY [CustomerId] LIMIT @BatchSize OFFSET @Offset")
                    .IncludeOptions(options =>
                    {
                        options.BatchSize = 10;
                        options.AddParameters(() =>
                            Enumerable
                                .Range(0, 5)
                                .Select(x => new ExtractParameterCollection(new ExtractParameter($"Parameter", typeof(int), x * 10)))
                                .ToList());
                    })
                    .WithOffsetPagination()
                    .Transform<Customer>((source, mapper) => mapper.Apply(source), mapper => Customer.ConfigureMapper(mapper))
                    .LoadSqlite(SqliteTest.ConnectionString)
                    .CopyRows();
        }
    }
}
