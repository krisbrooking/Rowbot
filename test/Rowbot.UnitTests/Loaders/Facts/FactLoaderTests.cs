using Microsoft.Extensions.Logging.Abstractions;
using Rowbot.Entities;
using Rowbot.Framework.Blocks.Connectors.Find;
using Rowbot.Framework.Blocks.Connectors.Synchronisation;
using Rowbot.Framework.Blocks.Transformers.Mappers.Transforms;
using Rowbot.Loaders.Facts;
using Rowbot.UnitTests.Connectors.DataTable;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Rowbot.UnitTests.Loaders.Facts
{
    public class FactLoaderTests
    {
        public FactLoaderTests()
        {
            DataTableProvider.Instance.Clear("FactCustomers");
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

        private async Task<FactLoader<CustomerEntity>> SetupLoaderAsync(IEnumerable<CustomerEntity> dataAtSource)
        {
            var connector = new DataTableWriteConnector<CustomerEntity>(new Entity<CustomerEntity>(), new SharedLockManager(), new FinderProvider());
            await connector.InsertAsync(dataAtSource);

            var options = new FactLoaderOptions<CustomerEntity>();

            var loader = new FactLoader<CustomerEntity>(
                new NullLogger<FactLoader<CustomerEntity>>(),
                new Entity<CustomerEntity>());
            loader.Options = options;
            loader.Connector = connector;

            return loader;
        }

        [Table("FactCustomers")]
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
