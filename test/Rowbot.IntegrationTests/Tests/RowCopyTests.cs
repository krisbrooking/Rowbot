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

            await PipelineTest.RunPipelinesAsync<CustomerPipeline>().RunAsync();

            var rows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal(10, rows.Count());
        }

        [Fact]
        public async Task TwoBatches_Should_CopyRows()
        {
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(20));

            await PipelineTest.RunPipelinesAsync<CustomerPipeline>().RunAsync();

            var rows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal(20, rows.Count());
        }

        public class CustomerPipeline(IPipelineBuilder pipelineBuilder) : IPipeline
        {
            public Pipeline Copy() =>
                pipelineBuilder
                    .Extract<SourceCustomer>(builder => builder
                        .FromSqlite(SqliteTest.ConnectionString, "SELECT [CustomerId], [CustomerName], [Inactive] FROM [SourceCustomer]"),
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
