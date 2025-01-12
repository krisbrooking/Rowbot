using Rowbot.Connectors.Sqlite;
using Rowbot.IntegrationTests.Setup;
using Rowbot.Pipelines.Summary;
using Rowbot.IntegrationTests.Setup.Entities;

namespace Rowbot.IntegrationTests.Tests
{
    [Collection("IntegrationTests")]
    public class TransactionFactTests
    {
        public TransactionFactTests() 
        {
            SqliteTest.Reset();
        }

        [Fact]
        public async Task LoadSameRowMultipleTimes_Should_ResultInSingleRowAtTarget()
        {
            await SetupDimensionsAsync();

            await SqliteTest.WriteRowsAsync(SourceOrderLine.GetValidEntities(1, 1, 1, 1));
            await ExecutePipelinesAsync(typeof(OrderLinePipeline));
            await ExecutePipelinesAsync(typeof(OrderLinePipeline));

            var rows = await SqliteTest.ReadRowsAsync<OrderLine>();

            Assert.Single(rows);
        }

        private async Task<IEnumerable<PipelineSummary>> ExecutePipelinesAsync(params Type[] pipelines)
        {
            return await SqliteTest
                .BuildRunner(pipelines)
                .RunAsync();
        }

        private async Task SetupDimensionsAsync()
        {
            await SqliteTest.WriteRowsAsync(SourceProduct.GetValidEntities(10));
            await ExecutePipelinesAsync(typeof(ProductPipeline));

            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(10));
            await ExecutePipelinesAsync(typeof(CustomerPipeline));

            await SqliteTest.WriteRowsAsync(SourceOrder.GetValidEntities(10));
            await ExecutePipelinesAsync(typeof(OrderPipeline));
        }

        public class CustomerPipeline(IPipelineBuilder pipelineBuilder) : IPipeline
        {
            public Pipeline Load() =>
                pipelineBuilder
                    .Extract<SourceCustomer>(builder => builder
                        .FromSqlite(
                            SqliteTest.ConnectionString,
                            "SELECT [CustomerId], [CustomerName], [Inactive] FROM [SourceCustomer]"),
                        10)
                    .Apply<Customer>(mapper => Customer.ConfigureMapper(mapper))
                    .Load(builder => builder
                        .ToSqlite(SqliteTest.ConnectionString)
                        .WithSlowlyChangingDimension());
        }

        public class ProductPipeline(IPipelineBuilder pipelineBuilder) : IPipeline
        {
            public Pipeline Load() =>
                pipelineBuilder
                    .Extract<SourceProduct>(builder => builder
                        .FromSqlite(
                            SqliteTest.ConnectionString,
                            "SELECT [ProductId], [ProductName], [Price], [Cost] FROM [SourceProduct]"),
                        10)
                    .Apply<Product>(mapper => Product.ConfigureMapper(mapper))
                    .Load(builder => builder
                        .ToSqlite(SqliteTest.ConnectionString)
                        .WithSlowlyChangingDimension());
        }

        public class OrderPipeline(IPipelineBuilder pipelineBuilder) : IPipeline
        {
            public Pipeline Load() =>
                pipelineBuilder
                    .Extract<SourceOrder>(builder => builder
                        .FromSqlite(
                            SqliteTest.ConnectionString,
                            "SELECT [OrderId], [OrderDate] FROM [SourceOrder]"),
                        10)
                    .Apply<Order>(mapper => Order.ConfigureMapper(mapper))
                    .Load(builder => builder
                        .ToSqlite(SqliteTest.ConnectionString)
                        .WithSlowlyChangingDimension());
        }

        public class OrderLinePipeline(IPipelineBuilder pipelineBuilder) : IPipeline
        {
            public Pipeline Load() => 
                pipelineBuilder
                    .Extract<SourceOrderLine>(builder => builder
                        .FromSqlite(
                            SqliteTest.ConnectionString,
                            "SELECT [OrderLineId], [OrderId], [CustomerId], [ProductId], [Quantity] FROM [SourceOrderLine]"),
                        10)
                    .Apply<OrderLine>(mapper => OrderLine.ConfigureMapper(mapper))
                    .Load(builder => builder
                        .ToSqlite(SqliteTest.ConnectionString)
                        .WithFact());
        }
    }
}
