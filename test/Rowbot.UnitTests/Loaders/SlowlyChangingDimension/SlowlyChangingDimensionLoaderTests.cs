using Microsoft.Extensions.Logging.Abstractions;
using Rowbot.Common.Services;
using Rowbot.Entities;
using Rowbot.Connectors.Common.Find;
using Rowbot.Connectors.Common.Synchronisation;
using Rowbot.Transformers.Mappers.Transforms;
using Rowbot.UnitTests.Connectors.DataTable;
using Rowbot.UnitTests.Setup;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Rowbot.Loaders.SlowlyChangingDimension;

namespace Rowbot.UnitTests.Loaders.SlowlyChangingDimension
{
    public class SlowlyChangingDimensionLoaderTests
    {
        public SlowlyChangingDimensionLoaderTests()
        {
            DataTableProvider.Instance.Clear("SlowlyChangingDimensionCustomers");
        }

        [Fact]
        public async Task Execute_Should_ReturnNoRowsChanged_WhenNoDataAtSourceAndNoDataToLoad()
        {
            var loader = await SetupLoaderAsync(dataAtSource: Enumerable.Empty<CustomerEntity>());

            var result = await loader.LoadAsync(Array.Empty<CustomerEntity>());

            Assert.Empty(result.Inserts);
            Assert.Empty(result.Updates);
        }

        [Fact]
        public async Task Execute_Should_InsertRow_WhenNoDataAtSource()
        {
            var loader = await SetupLoaderAsync(dataAtSource: Enumerable.Empty<CustomerEntity>());

            var result = await loader.LoadAsync(GetSingleValidEntity());

            Assert.Single(result.Inserts);
            Assert.Empty(result.Updates);
        }

        [Fact]
        public async Task Execute_Should_NotInsertRow_WhenRowExistsAtSource()
        {
            var rows = GetMultipleValidEntities();
            var loader = await SetupLoaderAsync(dataAtSource: rows);

            var result = await loader.LoadAsync(rows);

            Assert.Empty(result.Inserts);
            Assert.Empty(result.Updates);
        }

        [Fact]
        public async Task Execute_Should_UpdateRow_ForUpdateType1Field()
        {
            var loader = await SetupLoaderAsync(dataAtSource: GetSingleValidEntity());

            var result = await loader.LoadAsync(GetSingleValidEntity()
                .Assign(x => x.DateOfBirth, new DateTime(2000, 01, 02)).ToArray());

            Assert.Empty(result.Inserts);
            Assert.Single(result.Updates);
            Assert.Equal("DateOfBirth,ChangeHash", string.Join(',', result.Updates.First(x => x.Row.Id == 1).ChangedFields.Select(x => x.Name)));
        }

        [Fact]
        public async Task Execute_Should_UpdateField_ForUpdateType1Field()
        {
            var loader = await SetupLoaderAsync(dataAtSource: GetSingleValidEntity());

            var result = await loader.LoadAsync(GetSingleValidEntity()
                .Assign(x => x.DateOfBirth, new DateTime(2000, 01, 02)).ToArray());

            Assert.Equal(new DateTime(2000, 01, 02), result.Updates.First().Row.DateOfBirth);
        }

        [Fact]
        public async Task Execute_Should_UpdateChangeHash_ForUpdateType1Field()
        {
            var loader = await SetupLoaderAsync(dataAtSource: GetSingleValidEntity());

            var result = await loader.LoadAsync(GetSingleValidEntity()
                .Assign(x => x.DateOfBirth, new DateTime(2000, 01, 02)).ToArray());

            Assert.NotEqual(GetSingleValidEntity().First().ChangeHash, result.Updates.First().Row.ChangeHash);
        }

        [Fact]
        public async Task Execute_Should_UpdateRowAndInsertNewRow_ForUpdateType2Field()
        {
            var loader = await SetupLoaderAsync(dataAtSource: GetSingleValidEntity());

            var result = await loader.LoadAsync(GetSingleValidEntity()
                .Assign(x => x.Name, "Andrea").ToArray());

            Assert.Single(result.Inserts);
            Assert.Single(result.Updates);
        }

        [Fact]
        public async Task Execute_Should_MarkOldRowAsInactive_ForUpdateType2Field()
        {
            var loader = await SetupLoaderAsync(dataAtSource: GetSingleValidEntity());

            var result = await loader.LoadAsync(GetSingleValidEntity()
                .Assign(x => x.Name, "Andrea").ToArray());

            Assert.False(result.Updates.First(x => x.Row.Id == 1).Row.IsActive);
        }

        [Fact]
        public async Task Execute_Should_InsertNewRowWithChangedField_ForUpdateType2Field()
        {
            var loader = await SetupLoaderAsync(dataAtSource: GetSingleValidEntity());

            var result = await loader.LoadAsync(GetSingleValidEntity()
                .Assign(x => x.Name, "Andrea").ToArray());

            Assert.Equal("Andrea", result.Inserts.First(x => x.Id == 1).Name);
        }

        [Fact]
        public async Task Execute_Should_UpdateRowAndInsertNewRow_ForUpdateType1And2Field()
        {
            var loader = await SetupLoaderAsync(dataAtSource: GetSingleValidEntity());

            var result = await loader.LoadAsync(GetSingleValidEntity()
                .Assign(x => x.Name, "Andrea")
                .Assign(x => x.DateOfBirth, new DateTime(2000, 01, 02)).ToArray());

            Assert.Single(result.Inserts);
            Assert.Single(result.Updates);
            Assert.Equal("IsActive,ToDate,ToDateKey", string.Join(',', result.Updates.First(x => x.Row.Id == 1).ChangedFields.Select(x => x.Name)));
        }

        [Fact]
        public async Task Execute_Should_InsertNewRowWithChangedFields_ForUpdateType1And2Field()
        {
            var loader = await SetupLoaderAsync(dataAtSource: GetSingleValidEntity());

            var result = await loader.LoadAsync(GetSingleValidEntity()
                .Assign(x => x.Name, "Andrea")
                .Assign(x => x.DateOfBirth, new DateTime(2000, 01, 02)).ToArray());

            Assert.Equal("Andrea", result.Inserts.First(x => x.Id == 1).Name);
            Assert.Equal(new DateTime(2000, 01, 02), result.Inserts.First(x => x.Id == 1).DateOfBirth);
        }

        [Fact]
        public async Task Execute_Should_UpdateSingleRow_WhenRowDeleted()
        {
            var loader = await SetupLoaderAsync(dataAtSource: GetSingleValidEntity());

            var result = await loader.LoadAsync(GetSingleValidEntity().Assign(x => x.IsDeleted, true).ToArray());

            Assert.Single(result.Updates);
        }

        [Fact]
        public async Task Execute_Should_UpdateRowSettingFieldsForDeletion_WhenRowDeleted()
        {
            var rows = GetSingleValidEntity().Assign(x => x.IsDeleted, true);
            var loader = await SetupLoaderAsync(dataAtSource: GetSingleValidEntity());

            var result = await loader.LoadAsync(rows.ToArray());

            Assert.Equal("IsDeleted,ToDate,ToDateKey", string.Join(',', result.Updates.First(x => x.Row.Id == 1).ChangedFields.Select(x => x.Name)));
        }

        [Fact]
        public async Task Execute_Should_NotChangeUnchangedRow_WhenUpdateSingleRow()
        {
            var loader = await SetupLoaderAsync(dataAtSource: GetMultipleValidEntities());

            var result = await loader.LoadAsync(GetSingleValidEntity()
                .Assign(x => x.DateOfBirth, new DateTime(2000, 01, 02)).ToArray());

            Assert.Empty(result.Inserts);
            Assert.Single(result.Updates);
            Assert.Equal(new DateTime(2000, 01, 02), result.Updates.First(x => x.Row.Id == 1).Row.DateOfBirth);
        }

        private async Task<SlowlyChangingDimensionLoader<CustomerEntity>> SetupLoaderAsync(IEnumerable<CustomerEntity> dataAtSource)
        {
            var connector = new DataTableWriteConnector<CustomerEntity>(new Entity<CustomerEntity>(), new SharedLockManager(), new FinderProvider());
            await connector.InsertAsync(dataAtSource);

            var options = new SlowlyChangingDimensionLoaderOptions<CustomerEntity>();

            var loader = new SlowlyChangingDimensionLoader<CustomerEntity>(
                new NullLogger<SlowlyChangingDimensionLoader<CustomerEntity>>(),
                new Entity<CustomerEntity>(),
                new SystemClock());
            loader.Options = options;
            loader.Connector = connector;

            return loader;
        }

        [Table("SlowlyChangingDimensionCustomers")]
        public class CustomerEntity : Dimension
        {
            private Func<CustomerEntity, byte[]> _changeHashCodeGenerator;
            private string? _name;
            private DateTime? _dateOfBirth;

            public CustomerEntity()
            {
                _changeHashCodeGenerator = BuildHashCodeGenerator();
            }
            public CustomerEntity(int id, string name, DateTime? dateOfBirth) : this()
            {
                CustomerKey = id;
                Id = id;
                _name = name;
                _dateOfBirth = dateOfBirth;

                var hashCodeTransform = new HashCodeTransform<CustomerEntity>();
                var keyHashCodeGenerator = hashCodeTransform.Include(x => x.Id).Build();

                KeyHash = keyHashCodeGenerator(this);
                ChangeHash = _changeHashCodeGenerator(this);
            }

            [Key]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int CustomerKey { get; set; }

            public int Id { get; set; }

            [SlowlyChangingDimensionType2]
            public string? Name
            {
                get { return _name; }
                set { _name = value; ChangeHash = _changeHashCodeGenerator(this); }
            }

            public DateTime? DateOfBirth
            {
                get { return _dateOfBirth; }
                set { _dateOfBirth = value; ChangeHash = _changeHashCodeGenerator(this); }
            }

            private Func<CustomerEntity, byte[]> BuildHashCodeGenerator()
            {
                var hashCodeTransform = new HashCodeTransform<CustomerEntity>();
                return hashCodeTransform.All().Build();
            }
        }

        private CustomerEntity[] GetSingleValidEntity() => new CustomerEntity[] { new CustomerEntity(1, "Alice", new DateTime(2000, 01, 01)) };
        private CustomerEntity[] GetMultipleValidEntities() => new CustomerEntity[] { new CustomerEntity(1, "Alice", new DateTime(2000, 01, 01)), new CustomerEntity(2, "Bob", new DateTime(1970, 01, 01)) };
    }
}
