using Rowbot.IntegrationTests.Setup.Entities;
using Rowbot.Connectors.Sqlite;
using Rowbot.IntegrationTests.Setup;

namespace Rowbot.IntegrationTests.Tests
{
    [Collection("IntegrationTests")]
    public class TaskTests
    {
        public TaskTests()
        {
            SqliteTest.Reset();
        }

        [Fact]
        public async Task PrePipelineCommand_Should_CopyRows()
        {
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(10));

            var result = await PipelineTest.RunPipelinesAsync<CustomerPipeline>().RunAsync();

            var rows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal(10, rows.Count());
            Assert.Contains(result.SelectMany(x => x.BlockSummaries), x => x.Name == "Create Index");
            Assert.True(result.SelectMany(x => x.BlockSummaries).All(x => x.HasCompletedWithoutError));
        }

        public class CustomerPipeline(IPipelineBuilder pipelineBuilder) : IPipeline
        {
            public Pipeline Copy() =>
                pipelineBuilder
                    .Extract<SourceCustomer>(builder => builder
                        .FromSqlite(
                            SqliteTest.ConnectionString,
                            "SELECT [CustomerId], [CustomerName], [Inactive] FROM [SourceCustomer]"),
                        options: new ExtractOptions(batchSize: 10))
                    .Transform(source => source.Select(x => new Customer(x.CustomerId, x.CustomerName, x.Inactive, x.Source)))
                    .Apply<Customer>(mapper => mapper
                        .Transform.ToHashCode(hash => hash.WithSeed(1).Include(x => x.Id), x => x.KeyHash)
                        .Transform.ToHashCode(hash => hash.WithSeed(1).All(), x => x.ChangeHash))
                    .Load(builder => builder
                        .ToSqlite(SqliteTest.ConnectionString)
                        .WithTask(async (connector, logger) =>
                        {
                            await connector.ExecuteCommandAsync(
                                "CREATE INDEX IF NOT EXISTS idx_CustomerName ON SourceCustomer(CustomerName)");
                        }, "Create Index"));
        }
    }
}
