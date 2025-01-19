using Rowbot.Connectors.Sqlite;
using Rowbot.Pipelines.Summary;
using Rowbot.IntegrationTests.Setup;
using Rowbot.IntegrationTests.Setup.Entities;

namespace Rowbot.IntegrationTests.Tests
{
    [Collection("IntegrationTests")]
    public class SnapshotFactTests
    {
        public SnapshotFactTests() 
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

        [Fact]
        public async Task UpdateRow_Should_ResultInSingleUpdatedRowAtTarget()
        {
            await SetupDimensionsAsync();

            // Seed the order line with Quantity = 8
            await SqliteTest.WriteRowsAsync(SourceOrderLine.GetValidEntities(10, 10, 10, 10).AssignWhere(x => x.OrderLineId == 1, x => x.Quantity, 8));
            await ExecutePipelinesAsync(typeof(OrderLinePipeline));
            var originalRows = await SqliteTest.ReadRowsAsync<OrderLine>();

            Assert.Equal(8, originalRows.First(x => x.Id == 1).Quantity);

            // Drop SourceOrderLine table and reseed with the same order line except Quantity = 5, then load
            SqliteTest.Reset(nameof(SourceOrderLine));
            await SqliteTest.WriteRowsAsync(SourceOrderLine.GetValidEntities(10, 10, 10, 10).AssignWhere(x => x.OrderLineId == 1, x => x.Quantity, 5));
            await ExecutePipelinesAsync(typeof(OrderLinePipeline));
            var finalRows = await SqliteTest.ReadRowsAsync<OrderLine>();

            Assert.Equal(10, finalRows.Count());
            Assert.Equal(5, finalRows.First(x => x.Id == 1).Quantity);
            Assert.NotEqual(originalRows.First(x => x.Id == 1).ChangeHash, finalRows.First(x => x.Id == 1).ChangeHash);
        }

        [Fact]
        public async Task DeleteEntity_Should_MarkEntityIsDeleted()
        {
            await SetupDimensionsAsync();

            // Seed the order line
            await SqliteTest.WriteRowsAsync(SourceOrderLine.GetValidEntities(10, 10, 10, 10));
            await ExecutePipelinesAsync(typeof(OrderLinePipeline));

            // Drop OrderLine table and reseed with fewer rows (simulating rows deleted at source)
            SqliteTest.Reset(nameof(SourceOrderLine));
            await SqliteTest.WriteRowsAsync(SourceOrderLine.GetValidEntities(5, 10, 10, 10));
            await ExecutePipelinesAsync(typeof(OrderLinePipeline));
            var finalRows = await SqliteTest.ReadRowsAsync<OrderLine>();

            Assert.Equal(5, finalRows.Count(x => x.Id > 5 && x.IsDeleted));
        }

        [Fact]
        public async Task RecreateDeletedEntity_Should_ReuseDeletedEntity()
        {
            await SetupDimensionsAsync();

            // Seed the order line
            await SqliteTest.WriteRowsAsync(SourceOrderLine.GetValidEntities(10, 10, 10, 10));
            await ExecutePipelinesAsync(typeof(OrderLinePipeline));

            // Drop OrderLine table and reseed with fewer rows (simulating rows deleted at source)
            SqliteTest.Reset(nameof(SourceOrderLine));
            await SqliteTest.WriteRowsAsync(SourceOrderLine.GetValidEntities(5, 10, 10, 10));
            await ExecutePipelinesAsync(typeof(OrderLinePipeline));

            // Drop SourceCustomer table and reseed with original data (simulating transient issue at source)
            SqliteTest.Reset(nameof(SourceOrderLine));
            await SqliteTest.WriteRowsAsync(SourceOrderLine.GetValidEntities(10, 10, 10, 10));
            await ExecutePipelinesAsync(typeof(OrderLinePipeline));

            var finalRows = await SqliteTest.ReadRowsAsync<OrderLine>();

            Assert.Equal(10, finalRows.Count(x => !x.IsDeleted));
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
                        options: new ExtractOptions(batchSize: 10))
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
                        options: new ExtractOptions(batchSize: 10))
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
                        options: new ExtractOptions(batchSize: 10))
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
                        options: new ExtractOptions(batchSize: 10))
                    .Apply<OrderLine>(mapper => OrderLine.ConfigureMapper(mapper))
                    .Load(builder => builder
                        .ToSqlite(SqliteTest.ConnectionString)
                        .WithSnapshotFact());

            public Pipeline LoadDeletes() => 
                pipelineBuilder
                    .Extract<OrderLine>(builder => builder
                        .FromSqlite(
                            SqliteTest.ConnectionString,
                            @"
                            SELECT
                                orderLine.[Id],
                                orderLine.[KeyHash],
                                orderLine.[ChangeHash]
                            FROM [OrderLine] AS orderLine
                            LEFT JOIN [SourceOrderLine] AS source ON orderLine.[Id] = source.[OrderLineId]
                            WHERE 
	                            orderLine.[IsDeleted] = 0 AND
	                            source.[OrderLineId] IS NULL"),
                        options: new ExtractOptions(batchSize: 10))
                    .Transform<OrderLine>(source => 
                        source.Select(x => new OrderLine()
                        {
                            Id = x.Id,
                            KeyHash = x.KeyHash,
                            ChangeHash = x.ChangeHash,
                            IsDeleted = true
                        }).ToArray())
                    .Load(builder => builder
                        .ToSqlite(SqliteTest.ConnectionString)
                        .WithSnapshotFact());
        }
    }
}
