using Rowbot.Connectors.Sqlite;
using Rowbot.Pipelines.Summary;
using Rowbot.IntegrationTests.Setup;
using Rowbot.IntegrationTests.Setup.Entities;

namespace Rowbot.IntegrationTests.Tests
{
    [Collection("IntegrationTests")]
    public class SlowlyChangingDimensionTests
    {
        public SlowlyChangingDimensionTests() 
        {
            SqliteTest.Reset();
        }

        [Fact]
        public async Task LoadSameRowMultipleTimes_Should_ResultInSingleRowAtTarget()
        {
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(1));

            await ExecutePipelinesAsync(typeof(CustomerPipeline));
            await ExecutePipelinesAsync(typeof(CustomerPipeline));

            var rows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Single(rows);
        }

        [Fact]
        public async Task UpdateRowType1_Should_ResultInSingleUpdatedRowAtTarget()
        {
            // Seed the customer with Inactive = false
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(5).AssignWhere(x => x.CustomerId == 1, x => x.Inactive, false));
            await ExecutePipelinesAsync(typeof(CustomerPipeline));
            var originalRows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.False(originalRows.First(x => x.Id == 1).Inactive);

            // Drop SourceCustomer table and reseed with the same customer except Inactive = true, then load
            SqliteTest.Reset(nameof(SourceCustomer));
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(5).AssignWhere(x => x.CustomerId == 1, x => x.Inactive, true));
            await ExecutePipelinesAsync(typeof(CustomerPipeline));
            var finalRows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal(5, finalRows.Count());
            Assert.True(finalRows.First(x => x.Id == 1).Inactive);
            Assert.NotEqual(originalRows.First(x => x.Id == 1).ChangeHash, finalRows.First(x => x.Id == 1).ChangeHash);
        }

        [Fact]
        public async Task UpdateRowType2_Should_ResultInTwoRowsAtTarget()
        {
            // Seed the customer with Name = "ABC Corp"
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(5).AssignWhere(x => x.CustomerId == 5, x => x.CustomerName, "ABC Corp"));
            await ExecutePipelinesAsync(typeof(CustomerPipeline));
            var originalRows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal("ABC Corp", originalRows.First(x => x.Id == 5).Name);

            // Drop SourceCustomer table and reseed with the same customer except CustomerName = "XYZ Corp", then load
            SqliteTest.Reset(nameof(SourceCustomer));
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(5).AssignWhere(x => x.CustomerId == 5, x => x.CustomerName, "XYZ Corp"));
            await ExecutePipelinesAsync(typeof(CustomerPipeline));
            var finalRows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal(6, finalRows.Count());
            Assert.Single(finalRows.Where(x => !x.IsActive));
            Assert.NotNull(finalRows.First(x => x.Id == 5 && !x.IsActive).ToDate);
            Assert.Null(finalRows.First(x => x.Id == 5 && x.IsActive).ToDate);
            Assert.Equal(finalRows.First(x => x.Id == 5 && x.IsActive).KeyHash, finalRows.Last(x => x.Id == 5 && !x.IsActive).KeyHash);
        }

        // Microsoft.Data.Sqlite doesn't support ambient transactions so this tests data state correction rather than transaction rollback.
        [Fact]
        public async Task FailureUpdatingRowType2_Should_SelfCorrectOnSubsequentExecution()
        {
            // Seed the customer with Name = "ABC Corp"
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(1).Assign(x => x.CustomerName, "ABC Corp"));
            await ExecutePipelinesAsync(typeof(CustomerPipeline));
            var originalRows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal("ABC Corp", originalRows.First().Name);

            // Drop SourceCustomer table and attempt to update name to a string too long for dimension column constraint
            // Updating the row to IsActive = false is successful but insertion of the new row fails leaving the dimension in an inconsistent state (although data is not corrupt).
            try
            {
                SqliteTest.Reset(nameof(SourceCustomer));
                await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(1).Assign(x => x.CustomerName, new string(Enumerable.Range(0, 101).Select(x => '0').ToArray())));
                await ExecutePipelinesAsync(typeof(CustomerPipeline));
            }
            catch { }

            // Drop SourceCustomer table with Name = "XYZ Corp" (valid data). Dimension state is corrected by inserting the new row.
            SqliteTest.Reset(nameof(SourceCustomer));
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(1).Assign(x => x.CustomerName, "XYZ Corp"));
            await ExecutePipelinesAsync(typeof(CustomerPipeline));
            var finalRows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal(2, finalRows.Count());
            Assert.Single(finalRows.Where(x => x.IsActive));
            Assert.NotNull(finalRows.First(x => !x.IsActive).ToDate);
            Assert.Null(finalRows.First(x => x.IsActive).ToDate);
            Assert.Equal(finalRows.First().KeyHash, finalRows.Last().KeyHash);
        }

        [Fact]
        public async Task DeleteEntity_Should_MarkEntityIsDeleted()
        {
            // Seed the customer
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(10));
            await ExecutePipelinesAsync(typeof(CustomerPipeline));

            // Drop SourceCustomer table and reseed with fewer rows (simulating rows deleted at source)
            SqliteTest.Reset(nameof(SourceCustomer));
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(5));
            await ExecutePipelinesAsync(typeof(CustomerPipeline));
            var finalRows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal(5, finalRows.Count(x => x.Id > 5 && x.IsDeleted && x.ToDate is not null));
        }

        [Fact]
        public async Task RecreateDeletedEntity_Should_ReuseDeletedEntity()
        {
            // Seed the customer
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(10));
            await ExecutePipelinesAsync(typeof(CustomerPipeline));

            // Drop SourceCustomer table and reseed with fewer rows (simulating rows deleted at source)
            SqliteTest.Reset(nameof(SourceCustomer));
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(5));
            await ExecutePipelinesAsync(typeof(CustomerPipeline));

            // Drop SourceCustomer table and reseed with original data (simulating transient issue at source)
            SqliteTest.Reset(nameof(SourceCustomer));
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(10));
            await ExecutePipelinesAsync(typeof(CustomerPipeline));

            var finalRows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal(10, finalRows.Count(x => !x.IsDeleted && x.ToDate is null));
        }

        [Fact]
        public async Task OverridingDeletedEntityWithIsActiveFalse_Should_NotSetIsDeleted()
        {
            // Seed the customer
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(10));
            await ExecutePipelinesAsync(typeof(CustomerPipeline));

            // Drop SourceCustomer table and reseed with fewer rows (simulating rows deleted at source)
            SqliteTest.Reset(nameof(SourceCustomer));
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(5));
            await ExecutePipelinesAsync(typeof(CustomerPipelineOverrideDeleteWithIsActiveFalse));

            var finalRows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal(5, finalRows.Count(x => x.Id > 5 && !x.IsDeleted && !x.IsActive && x.ToDate is not null));
        }

        [Fact]
        public async Task RecreateInactiveEntity_Should_CreateNewEntity()
        {
            // Seed the customer
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(10));
            await ExecutePipelinesAsync(typeof(CustomerPipeline));

            // Drop SourceCustomer table and reseed with fewer rows (simulating rows deleted at source)
            SqliteTest.Reset(nameof(SourceCustomer));
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(5));
            await ExecutePipelinesAsync(typeof(CustomerPipelineOverrideDeleteWithIsActiveFalse));

            // Drop SourceCustomer table and reseed with original data (simulating transient issue at source)
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(10));
            await ExecutePipelinesAsync(typeof(CustomerPipeline));

            var finalRows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal(15, finalRows.Count());
            Assert.Equal(10, finalRows.Count(x => x.IsActive));
        }

        [Fact]
        public async Task CanSetAdditionalFieldsWhenEntityIsDeleted()
        {
            // Seed the customer
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(10));
            await ExecutePipelinesAsync(typeof(CustomerPipeline));

            // Drop SourceCustomer table and reseed with fewer rows (simulating rows deleted at source)
            SqliteTest.Reset(nameof(SourceCustomer));
            await SqliteTest.WriteRowsAsync(SourceCustomer.GetValidEntities(5));
            await ExecutePipelinesAsync(typeof(CustomerPipelineOverrideDeleteWithIsActiveFalse));

            var finalRows = await SqliteTest.ReadRowsAsync<Customer>();

            Assert.Equal(5, finalRows.Count(x => x.Id > 5 && x.Inactive));
        }

        private async Task<IEnumerable<PipelineSummary>> ExecutePipelinesAsync(params Type[] pipelines) =>
            await SqliteTest
                .BuildRunner(pipelines)
                .RunAsync();

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

            public Pipeline LoadDeletes() =>
                pipelineBuilder
                    .Extract<Customer>(builder => builder
                        .FromSqlite(
                            SqliteTest.ConnectionString,
                            @"
                            SELECT
                                customer.[Id],
                                customer.[KeyHash],
                                customer.[ChangeHash]
                            FROM [Customer] AS customer
                            LEFT JOIN [SourceCustomer] AS source ON customer.[Id] = source.[CustomerId]
                            WHERE 
	                            customer.[IsDeleted] = 0 AND
	                            source.[CustomerId] IS NULL"),
                        10)
                    .Transform<Customer>(source => 
                        source.Select(x => new Customer()
                        {
                            Id = x.Id,
                            KeyHash = x.KeyHash,
                            ChangeHash = x.ChangeHash,
                            IsDeleted = true
                        }).ToArray())
                    .Load(builder => builder
                        .ToSqlite(SqliteTest.ConnectionString)
                        .WithSlowlyChangingDimension());
        }

        public class CustomerPipelineOverrideDeleteWithIsActiveFalse(IPipelineBuilder pipelineBuilder) : IPipeline
        {
            public Pipeline LoadDeletes() =>
                pipelineBuilder
                    .Extract<Customer>(builder => builder
                        .FromSqlite(
                            SqliteTest.ConnectionString,
                            @"
                            SELECT
                                customer.[Id],
                                customer.[KeyHash],
                                customer.[ChangeHash]
                            FROM [Customer] AS customer
                            LEFT JOIN [SourceCustomer] AS source ON customer.[Id] = source.[CustomerId]
                            WHERE 
	                            customer.[IsDeleted] = 0 AND
	                            source.[CustomerId] IS NULL"),
                        10)
                    .Transform<Customer>(source => 
                        source.Select(x => new Customer()
                        {
                            Id = x.Id,
                            KeyHash = x.KeyHash,
                            ChangeHash = x.ChangeHash,
                            Inactive = true,
                            IsDeleted = true
                        }).ToArray())
                    .Load(builder => builder
                        .ToSqlite(SqliteTest.ConnectionString)
                        .WithSlowlyChangingDimension(options =>
                        {
                            options.OverrideDeleteWithIsActiveFalse = true;
                            options.SetFieldsToUpdateOnDelete(x => x.Include(x => x.Inactive));
                        }));
        }
    }
}
