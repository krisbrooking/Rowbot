using Rowbot.Connectors.Sqlite;
using Rowbot.IntegrationTests.Setup;
using Rowbot.Pipelines.Summary;
using Rowbot.IntegrationTests.Setup.Entities;

namespace Rowbot.IntegrationTests.Tests
{
    [Collection("IntegrationTests")]
    public class MultipleSourcesTests
    {
        public MultipleSourcesTests()
        {
            SqliteTest.Reset();
        }

        [Fact]
        public async Task MultipleSourceEntitiesWithSameIdButDifferentKeyHashes_Should_CreateSeparateDimensionRows()
        {
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(10));
            await SqliteTest.WriteRowsAsync(SecondSourceCustomer.GetValidEntities(10));

            await PipelineTest.RunPipelinesAsync<CustomerPipeline>().RunAsync();
            await PipelineTest.RunPipelinesAsync<SecondCustomerPipeline>().RunAsync();
            var rows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal(20, rows.Count());
        }

        [Fact]
        public async Task MultipleSourceEntitiesWithMapper_Should_NotCreateSecondDimensionRow()
        {
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(10));
            await SqliteTest.WriteRowsAsync(SecondSourceCustomer.GetValidEntities(10));

            await PipelineTest.RunPipelinesAsync<CustomerPipeline>().RunAsync();
            await PipelineTest.RunPipelinesAsync<SecondCustomerWithMapperPipeline>().RunAsync();
            var rows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal(19, rows.Count());
            Assert.Single(rows, x => x.Id == 1);
        }

        [Fact]
        public async Task MultipleDimensionEntitiesWithMapper_Should_IntegrateSecondDimensionRow()
        {
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(10));
            await SqliteTest.WriteRowsAsync(SecondSourceCustomer.GetValidEntities(10));

            await PipelineTest.RunPipelinesAsync<CustomerPipeline>().RunAsync();
            await PipelineTest.RunPipelinesAsync<SecondCustomerPipeline>().RunAsync();
            var originalRows = await SqliteTest.ReadRowsAsync<Customer>();

            await PipelineTest.RunPipelinesAsync<SecondCustomerWithMapperPipeline>().RunAsync();
            var finalRows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal(20, originalRows.Count());
            Assert.Equal(2, originalRows.Count(x => x.IntegrationId == 1 && x.IsActive));
            Assert.Equal(20, finalRows.Count());
            Assert.Single(finalRows, x => x.Id == 1 && x.IntegrationId == 1 && x.Source == 1 && x.IsActive);
            Assert.Single(finalRows, x => x.Id == 1 && x.IntegrationId == 1 && x.Source == 1 && !x.IsActive);
        }

        public class CustomerPipeline(IPipelineBuilder pipelineBuilder) : IPipeline
        {
            public Pipeline Load() =>
                pipelineBuilder
                    .Extract<SourceCustomer>(builder => builder
                        .FromSqlite(SqliteTest.ConnectionString, "SELECT [CustomerId], [CustomerName], [Inactive] FROM [SourceCustomer]"),
                        options: new ExtractOptions(batchSize: 10))
                    .Transform(source => source.Select(x => new Customer(x.CustomerId, x.CustomerName, x.Inactive, x.Source)))
                    .Apply<Customer>(mapper => mapper
                        .Transform.ToHashCode(hash => hash.WithSeed(1).Include(x => x.Id), x => x.KeyHash)
                        .Transform.ToHashCode(hash => hash.WithSeed(1).All(), x => x.ChangeHash))
                    .Load(builder => builder
                        .ToSqlite(SqliteTest.ConnectionString)
                        .WithSlowlyChangingDimension());
        }

        public class SecondCustomerPipeline(IPipelineBuilder pipelineBuilder) : IPipeline
        {
            public Pipeline Load() =>
                pipelineBuilder
                    .Extract<SecondSourceCustomer>(builder => builder
                        .FromSqlite(SqliteTest.ConnectionString, "SELECT [CustomerId], [CustomerName], [Inactive] FROM [SecondSourceCustomer]"),
                        options: new ExtractOptions(batchSize: 10))
                    .Transform(source => source.Select(x => new Customer(x.CustomerId, x.CustomerName, x.Inactive, x.Source)))
                    .Apply<Customer>(mapper => mapper
                        .Transform.ToHashCode(hash => hash.WithSeed(2).Include(x => x.Id), x => x.KeyHash)
                        .Transform.ToHashCode(hash => hash.WithSeed(2).All(), x => x.ChangeHash))
                    .Load(builder => builder
                        .ToSqlite(SqliteTest.ConnectionString)
                        .WithSlowlyChangingDimension());
        }

        public class SecondCustomerWithMapperPipeline(IPipelineBuilder pipelineBuilder) : IPipeline
        {
            public Pipeline Load() =>
                pipelineBuilder
                    .Extract<SecondSourceCustomer>(builder => builder
                        .FromSqlite(
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
                            "),
                        options: new ExtractOptions(batchSize: 10))
                    .Transform(source => source.Select(x => new Customer(x.CustomerId, x.CustomerName, x.Inactive, x.Source)))
                    .Apply<Customer>(mapper => mapper
                        .Transform.ToHashCode(hash => hash.WithSeed(2).Include(x => x.Id), x => x.KeyHash)
                        .Transform.ToHashCode(hash => hash.WithSeed(2).All(), x => x.ChangeHash))
                    .Load(builder => builder
                        .ToSqlite(SqliteTest.ConnectionString)
                        .WithSlowlyChangingDimension());

            public Pipeline LoadDeletes() =>
                pipelineBuilder
                    .Extract<Customer>(builder => builder
                        .FromSqlite(
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
	                            (second.[CustomerId] IS NULL OR mapping.[SourceCustomerId] IS NOT NULL)"),
                        options: new ExtractOptions(batchSize: 10))
                    .Transform<Customer>(source =>
                        source.Select(x => new Customer()
                        {
                            Source = 1,
                            Id = x.Id,
                            IntegrationId = x.IntegrationId,
                            KeyHash = x.KeyHash,
                            ChangeHash = x.ChangeHash,
                            IsDeleted = true
                        }).ToArray())
                    .Load(builder => builder
                        .ToSqlite(SqliteTest.ConnectionString)
                        .WithSlowlyChangingDimension(options =>
                        {
                            options.OverrideDeleteWithIsActiveFalse = true;
                            options.SetFieldsToUpdateOnDelete(x => x.Include(x => x.IntegrationId).Include(x => x.Source));
                        }));
        }
    }
}
