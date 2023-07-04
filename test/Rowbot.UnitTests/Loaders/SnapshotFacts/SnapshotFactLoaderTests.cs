using Microsoft.Extensions.Logging.Abstractions;
using Rowbot.Entities;
using Rowbot.Framework.Blocks.Connectors.Find;
using Rowbot.Framework.Blocks.Connectors.Synchronisation;
using Rowbot.Framework.Blocks.Transformers.Mappers.Transforms;
using Rowbot.Loaders.SnapshotFacts;
using Rowbot.UnitTests.Connectors.DataTable;
using Rowbot.UnitTests.Setup;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Rowbot.UnitTests.Loaders.SnapshotFacts
{
    public class SnapshotFactLoaderTests
    {
        public SnapshotFactLoaderTests()
        {
            DataTableProvider.Instance.Clear("SnapshotFactCustomers");
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
        public async Task Execute_Should_UpdateRow()
        {
            var loader = await SetupLoaderAsync(dataAtSource: GetSingleValidEntity());

            var result = await loader.LoadAsync(GetSingleValidEntity()
                .Assign(x => x.Name, "Andrea").ToArray());

            Assert.Empty(result.Inserts);
            Assert.Single(result.Updates);
            Assert.Equal("Name,ChangeHash", string.Join(',', result.Updates.First(x => x.Row.Id == 1).ChangedFields.Select(x => x.Name)));
        }

        [Fact]
        public async Task Execute_Should_UpdateField()
        {
            var loader = await SetupLoaderAsync(dataAtSource: GetSingleValidEntity());

            var result = await loader.LoadAsync(GetSingleValidEntity()
                .Assign(x => x.Name, "Andrea").ToArray());

            Assert.Equal("Andrea", result.Updates.First().Row.Name);
        }

        [Fact]
        public async Task Execute_Should_UpdateChangeHash()
        {
            var loader = await SetupLoaderAsync(dataAtSource: GetSingleValidEntity());

            var result = await loader.LoadAsync(GetSingleValidEntity()
                .Assign(x => x.Name, "Andrea").ToArray());

            Assert.NotEqual(GetSingleValidEntity().First().ChangeHash, result.Updates.First().Row.ChangeHash);
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

            Assert.Equal("IsDeleted", string.Join(',', result.Updates.First(x => x.Row.Id == 1).ChangedFields.Select(x => x.Name)));
        }

        private async Task<SnapshotFactLoader<CustomerEntity>> SetupLoaderAsync(IEnumerable<CustomerEntity> dataAtSource)
        {
            var connector = new DataTableWriteConnector<CustomerEntity>(new Entity<CustomerEntity>(), new SharedLockManager(), new FinderProvider());
            await connector.InsertAsync(dataAtSource);

            var options = new SnapshotFactLoaderOptions<CustomerEntity>();

            var loader = new SnapshotFactLoader<CustomerEntity>(
                new NullLogger<SnapshotFactLoader<CustomerEntity>>(),
                new Entity<CustomerEntity>());
            loader.Options = options;
            loader.Connector = connector;

            return loader;
        }

        [Table("SnapshotFactCustomers")]
        public class CustomerEntity : Fact
        {
            private Func<CustomerEntity, byte[]> _changeHashCodeGenerator;
            private string? _name;

            public CustomerEntity()
            {
                _changeHashCodeGenerator = BuildHashCodeGenerator();
            }
            public CustomerEntity(int id, string name) : this()
            {
                CustomerKey = id;
                Id = id;
                _name = name;

                var hashCodeTransform = new HashCodeTransform<CustomerEntity>();
                var keyHashCodeGenerator = hashCodeTransform.Include(x => x.Id).Build();

                KeyHash = keyHashCodeGenerator(this);
                ChangeHash = _changeHashCodeGenerator(this);
            }

            [Key]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int CustomerKey { get; set; }

            public int Id { get; set; }

            public string? Name
            {
                get { return _name; }
                set { _name = value; ChangeHash = _changeHashCodeGenerator(this); }
            }

            private Func<CustomerEntity, byte[]> BuildHashCodeGenerator()
            {
                var hashCodeTransform = new HashCodeTransform<CustomerEntity>();
                return hashCodeTransform.All().Build();
            }
        }

        private CustomerEntity[] GetSingleValidEntity() => new CustomerEntity[] { new CustomerEntity(1, "Alice") };
        private CustomerEntity[] GetMultipleValidEntities() => new CustomerEntity[] { new CustomerEntity(1, "Alice"), new CustomerEntity(2, "Bob") };
    }
}
