using Microsoft.Extensions.Logging.Abstractions;
using Rowbot.Entities;
using Rowbot.Connectors.Common.Find;
using Rowbot.Connectors.Common.Synchronisation;
using Rowbot.Transformers.Mappers.Transforms;
using Rowbot.UnitTests.Connectors.DataTable;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Rowbot.Loaders.TransactionFact;

namespace Rowbot.UnitTests.Loaders.TransactionFact
{
    public class TransactionFactLoaderTests
    {
        public TransactionFactLoaderTests()
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

        private async Task<TransactionFactLoader<CustomerEntity>> SetupLoaderAsync(IEnumerable<CustomerEntity> dataAtSource)
        {
            var connector = new DataTableWriteConnector<CustomerEntity>(new Entity<CustomerEntity>(), new SharedLockManager(), new FinderProvider());
            await connector.InsertAsync(dataAtSource);

            var loader = new TransactionFactLoader<CustomerEntity>(
                new NullLogger<TransactionFactLoader<CustomerEntity>>(),
                new Entity<CustomerEntity>());
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
