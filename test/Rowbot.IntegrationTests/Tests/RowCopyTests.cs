using Rowbot.Connectors.Sqlite;
using Rowbot.IntegrationTests.Setup;
using Rowbot.IntegrationTests.Setup.Entities;

namespace Rowbot.IntegrationTests.Tests
{
    [Collection("IntegrationTests")]
    public class RowCopyTests
    {
        public RowCopyTests()
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

        public class CustomerPipeline(IPipelineBuilder pipelineBuilder) : IPipeline
        {
            public Pipeline Copy() =>
                pipelineBuilder
                    .Extract<SourceCustomer>(builder => builder
                        .FromSqlite(
                            SqliteTest.ConnectionString,
                            "SELECT [CustomerId], [CustomerName], [Inactive] FROM [SourceCustomer]"),
                        10)
                    .Apply<Customer>(mapper => Customer.ConfigureMapper(mapper))
                    .Load(builder => builder
                        .ToSqlite(SqliteTest.ConnectionString));
        }
    }
}
