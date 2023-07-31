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
    public class SqliteFactTests
    {
        public SqliteFactTests() 
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
                    .IncludeOptions(options => options.BatchSize = 10)
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
                    .IncludeOptions(options => options.BatchSize = 10)
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
                    .IncludeOptions(options => options.BatchSize = 10)
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
                    .IncludeOptions(options => options.BatchSize = 10)
                    .Transform<OrderLine>((source, mapper) => mapper.Apply(source), mapper => OrderLine.ConfigureMapper(mapper))
                    .LoadSqlite(SqliteTest.ConnectionString)
                    .WithFact();
        }
    }
}
