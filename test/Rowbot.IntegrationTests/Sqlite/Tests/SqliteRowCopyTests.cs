using Rowbot.IntegrationTests.Setup.Entities;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Rowbot.IntegrationTests.Sqlite.Tests
{
    [Collection("IntegrationTests")]
    public class SqliteRowCopyTests
    {
        public SqliteRowCopyTests()
        {
            SqliteTest.Reset();
        }

        [Fact]
        public async Task SingleBatch_Should_CopyRows()
        {
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(10));

            await SqliteTest
                .BuildRunner(typeof(CustomerPipeline))
                .RunAsync();

            var rows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal(10, rows.Count());
        }

        [Fact]
        public async Task TwoBatches_Should_CopyRows()
        {
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(20));

            await SqliteTest
                .BuildRunner(typeof(CustomerPipeline))
                .RunAsync();

            var rows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal(20, rows.Count());
        }

        public class CustomerPipeline : IPipelineContainer
        {
            private readonly IPipelineBuilder _pipelineBuilder;

            public CustomerPipeline(IPipelineBuilder pipelineBuilder)
            {
                _pipelineBuilder = pipelineBuilder;
            }

            public Pipeline Copy() =>
                _pipelineBuilder
                    .ExtractSqlite<SourceCustomer>(
                        SqliteTest.ConnectionString,
                        "SELECT [CustomerId], [CustomerName], [Inactive] FROM [SourceCustomer]")
                    .IncludeOptions(options => options.BatchSize = 10)
                    .Transform<Customer>((source, mapper) => mapper.Apply(source), mapper => Customer.ConfigureMapper(mapper))
                    .LoadSqlite(SqliteTest.ConnectionString)
                    .CopyRows();
        }
    }
}
