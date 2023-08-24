using Rowbot.Framework.Pipelines.Summary;
using Rowbot.IntegrationTests.Setup;
using Rowbot.IntegrationTests.Setup.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Rowbot.IntegrationTests.Sqlite.Tests
{
    [Collection("IntegrationTests")]
    public class SqliteSnapshotFactTests
    {
        public SqliteSnapshotFactTests() 
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

        public class CustomerPipeline : IPipelineContainer
        {
            private readonly IPipelineBuilder _pipelineBuilder;

            public CustomerPipeline(IPipelineBuilder pipelineBuilder)
            {
                _pipelineBuilder = pipelineBuilder;
            }

            public Pipeline Load() =>
                _pipelineBuilder
                    .ExtractSqlite<SourceCustomer>(
                        SqliteTest.ConnectionString,
                        "SELECT [CustomerId], [CustomerName], [Inactive] FROM [SourceCustomer]")
                    .WithDefaultExtractor(options => options.BatchSize = 10)
                    .Transform<Customer>((source, mapper) => mapper.Apply(source), mapper => Customer.ConfigureMapper(mapper))
                    .LoadSqlite(SqliteTest.ConnectionString)
                    .WithSlowlyChangingDimension();
        }

        public class ProductPipeline : IPipelineContainer
        {
            private readonly IPipelineBuilder _pipelineBuilder;

            public ProductPipeline(IPipelineBuilder pipelineBuilder)
            {
                _pipelineBuilder = pipelineBuilder;
            }

            public Pipeline Load() =>
                _pipelineBuilder
                    .ExtractSqlite<SourceProduct>(
                        SqliteTest.ConnectionString,
                        "SELECT [ProductId], [ProductName], [Price], [Cost] FROM [SourceProduct]")
                    .WithDefaultExtractor(options => options.BatchSize = 10)
                    .Transform<Product>((source, mapper) => mapper.Apply(source), mapper => Product.ConfigureMapper(mapper))
                    .LoadSqlite(SqliteTest.ConnectionString)
                    .WithSlowlyChangingDimension();
        }

        public class OrderPipeline : IPipelineContainer
        {
            private readonly IPipelineBuilder _pipelineBuilder;

            public OrderPipeline(IPipelineBuilder pipelineBuilder)
            {
                _pipelineBuilder = pipelineBuilder;
            }

            public Pipeline Load() =>
                _pipelineBuilder
                    .ExtractSqlite<SourceOrder>(SqliteTest.ConnectionString,
                        "SELECT [OrderId], [OrderDate] FROM [SourceOrder]")
                    .WithDefaultExtractor(options => options.BatchSize = 10)
                    .Transform<Order>((source, mapper) => mapper.Apply(source), mapper => Order.ConfigureMapper(mapper))
                    .LoadSqlite(SqliteTest.ConnectionString)
                    .WithSlowlyChangingDimension();
        }

        public class OrderLinePipeline : IPipelineContainer
        {
            private readonly IPipelineBuilder _pipelineBuilder;

            public OrderLinePipeline(IPipelineBuilder pipelineBuilder)
            {
                _pipelineBuilder = pipelineBuilder;
            }

            public Pipeline Load() => 
                _pipelineBuilder
                    .ExtractSqlite<SourceOrderLine>(
                        SqliteTest.ConnectionString,
                        "SELECT [OrderLineId], [OrderId], [CustomerId], [ProductId], [Quantity] FROM [SourceOrderLine]")
                    .WithDefaultExtractor(options => options.BatchSize = 10)
                    .Transform<OrderLine>((source, mapper) => mapper.Apply(source), mapper => OrderLine.ConfigureMapper(mapper))
                    .LoadSqlite(SqliteTest.ConnectionString)
                    .WithSnapshotFact();

            public Pipeline LoadDeletes() => 
                _pipelineBuilder
                    .ExtractSqlite<OrderLine>(
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
	                        source.[OrderLineId] IS NULL")
                    .WithDefaultExtractor(options => options.BatchSize = 10)
                    .Transform<OrderLine>(
                    (source, mapper) =>
                    {
                        return source.Select(x => new OrderLine()
                        {
                            Id = x.Id,
                            KeyHash = x.KeyHash,
                            ChangeHash = x.ChangeHash,
                            IsDeleted = true
                        }).ToArray();
                    },
                    mapperConfiguration => new MapperConfiguration<OrderLine, OrderLine>())
                    .LoadSqlite(SqliteTest.ConnectionString)
                    .WithSnapshotFact();
        }
    }
}
