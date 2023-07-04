using Rowbot.IntegrationTests.Setup.Entities;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Rowbot.IntegrationTests.Sqlite.Tests
{
    [Collection("IntegrationTests")]
    public class SqlitePrePostPipelineCommandTests
    {
        public SqlitePrePostPipelineCommandTests()
        {
            SqliteTest.Reset();
        }

        [Fact]
        public async Task PrePipelineCommand_Should_CopyRows()
        {
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(10));

            var result = await SqliteTest
                .BuildRunner(typeof(CustomerPipeline))
                .RunAsync();

            var rows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal(10, rows.Count());
            Assert.Equal(5, result.SelectMany(x => x.BlockSummaries).Count());
            Assert.True(result.SelectMany(x => x.BlockSummaries).All(x => x.HasCompletedWithoutError));
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
                    .LoadSqlite(SqliteTest.ConnectionString, options =>
                    {
                        options.AddPrePipelineCommand("CREATE INDEX IF NOT EXISTS idx_CustomerName ON SourceCustomer(CustomerName)");
                    })
                    .CopyRows();
        }
    }
}
