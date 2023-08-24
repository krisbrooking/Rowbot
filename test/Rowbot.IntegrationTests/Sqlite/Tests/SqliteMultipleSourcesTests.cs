using Rowbot.Framework.Pipelines.Summary;
using Rowbot.IntegrationTests.Setup.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Rowbot.IntegrationTests.Sqlite.Tests
{
    [Collection("IntegrationTests")]
    public class SqliteMultipleSourcesTests
    {
        public SqliteMultipleSourcesTests()
        {
            SqliteTest.Reset();
        }

        [Fact]
        public async Task MultipleSourceEntitiesWithSameIdButDifferentKeyHashes_Should_CreateSeparateDimensionRows()
        {
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(10));
            await SqliteTest.WriteRowsAsync(SecondSourceCustomer.GetValidEntities(10));

            await ExecutePipelinesAsync(typeof(CustomerPipeline));
            await ExecutePipelinesAsync(typeof(SecondCustomerPipeline));
            var rows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal(20, rows.Count());
        }

        [Fact]
        public async Task MultipleSourceEntitiesWithMapper_Should_NotCreateSecondDimensionRow()
        {
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(10));
            await SqliteTest.WriteRowsAsync(SecondSourceCustomer.GetValidEntities(10));

            await ExecutePipelinesAsync(typeof(CustomerPipeline));
            await ExecutePipelinesAsync(typeof(SecondCustomerWithMapperPipeline));
            var rows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal(19, rows.Count());
            Assert.Single(rows.Where(x => x.Id == 1));
        }

        [Fact]
        public async Task MultipleDimensionEntitiesWithMapper_Should_IntegrateSecondDimensionRow()
        {
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(10));
            await SqliteTest.WriteRowsAsync(SecondSourceCustomer.GetValidEntities(10));

            await ExecutePipelinesAsync(typeof(CustomerPipeline));
            await ExecutePipelinesAsync(typeof(SecondCustomerPipeline));
            var originalRows = await SqliteTest.ReadRowsAsync<Customer>();

            await ExecutePipelinesAsync(typeof(SecondCustomerWithMapperPipeline));
            var finalRows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal(20, originalRows.Count());
            Assert.Equal(2, originalRows.Count(x => x.IntegrationId == 1 && x.IsActive));
            Assert.Equal(20, finalRows.Count());
            Assert.Single(finalRows.Where(x => x.Id == 1 && x.IntegrationId == 1 && x.Source == 1 && x.IsActive));
            Assert.Single(finalRows.Where(x => x.Id == 1 && x.IntegrationId == 1 && x.Source == 1 && !x.IsActive));
        }

        private async Task<IEnumerable<PipelineSummary>> ExecutePipelinesAsync(params Type[] pipelines)
        {
            return await SqliteTest
                .BuildRunner(pipelines)
                .RunAsync();
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

        public class SecondCustomerPipeline : IPipelineContainer
        {
            private readonly IPipelineBuilder _pipelineBuilder;

            public SecondCustomerPipeline(IPipelineBuilder pipelineBuilder)
            {
                _pipelineBuilder = pipelineBuilder;
            }

            public Pipeline Load() =>
                _pipelineBuilder
                    .ExtractSqlite<SecondSourceCustomer>(
                        SqliteTest.ConnectionString,
                        "SELECT [CustomerId], [CustomerName], [Inactive] FROM [SecondSourceCustomer]")
                    .WithDefaultExtractor(options => options.BatchSize = 10)
                    .Transform<Customer>((source, mapper) => mapper.Apply(source), mapper => Customer.ConfigureMapper(mapper))
                    .LoadSqlite(SqliteTest.ConnectionString)
                    .WithSlowlyChangingDimension();
        }

        public class SecondCustomerWithMapperPipeline : IPipelineContainer
        {
            private readonly IPipelineBuilder _pipelineBuilder;

            public SecondCustomerWithMapperPipeline(IPipelineBuilder pipelineBuilder)
            {
                _pipelineBuilder = pipelineBuilder;
            }

            public Pipeline Load() =>
                _pipelineBuilder
                    .ExtractSqlite<SecondSourceCustomer>(
                        SqliteTest.ConnectionString,
                        @"
                        WITH MappingCTE AS (
	                        SELECT 1 AS [SourceCustomerId], 1 AS [SecondSourceCustomerId]
                        )

                        SELECT
	                        second.[CustomerId], 
	                        second.[CustomerName], 
	                        second.[Inactive] 
                        FROM [SecondSourceCustomer] AS second
                        LEFT JOIN MappingCTE AS mapping ON second.[CustomerId] = mapping.[SecondSourceCustomerId]
                        LEFT JOIN [SourceCustomer] AS source ON mapping.[SourceCustomerId] = source.[CustomerId]
                        WHERE
	                        source.[CustomerId] IS NULL
                        ")
                    .WithDefaultExtractor(options => options.BatchSize = 10)
                    .Transform<Customer>((source, mapper) => mapper.Apply(source), mapper => Customer.ConfigureMapper(mapper))
                    .LoadSqlite(SqliteTest.ConnectionString)
                    .WithSlowlyChangingDimension();

            public Pipeline LoadDeletes() =>
                _pipelineBuilder
                    .ExtractSqlite<Customer>(
                        SqliteTest.ConnectionString,
                        @"
                        WITH MappingCTE AS (
	                        SELECT 1 AS [SourceCustomerId], 1 AS [SecondSourceCustomerId]
                        )

                        SELECT
	                        customer.[Id],
	                        mapping.[SourceCustomerId] AS [IntegrationId],
	                        customer.[KeyHash],
	                        customer.[ChangeHash]
                        FROM [Customer] AS customer
                        LEFT JOIN [SourceCustomer] AS second ON customer.[Id] = second.[CustomerId]
                        LEFT JOIN MappingCTE AS mapping ON customer.[Id] = mapping.[SecondSourceCustomerId]
                        WHERE 
	                        customer.[IsActive] = 1 AND
	                        customer.[Source] = 2 AND
	                        (second.[CustomerId] IS NULL OR mapping.[SourceCustomerId] IS NOT NULL)")
                    .WithDefaultExtractor(options => options.BatchSize = 10)
                    .Transform<Customer>(
                        (source, mapper) =>
                        {
                            return source.Select(x => new Customer()
                            {
                                Source = 1,
                                Id = x.Id,
                                IntegrationId = x.IntegrationId,
                                KeyHash = x.KeyHash,
                                ChangeHash = x.ChangeHash,
                                IsDeleted = true
                            }).ToArray();
                        },
                        mapperConfiguration => new MapperConfiguration<Customer, Customer>())
                    .LoadSqlite(SqliteTest.ConnectionString)
                    .WithSlowlyChangingDimension(options =>
                    {
                        options.OverrideDeleteWithIsActiveFalse = true;
                        options.SetFieldsToUpdateOnDelete(x => x.Include(x => x.IntegrationId).Include(x => x.Source));
                    });
        }
    }
}
