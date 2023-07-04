using Bogus;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Rowbot.IntegrationTests.Setup.Entities
{
    public sealed class SourceOrderLine
    {
        public int OrderLineId { get; set; }
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }

        public static IEnumerable<SourceOrderLine> GetValidEntities(int count, int orderIdMax, int customerIdMax, int productIdMax, int seed = 1)
        {
            var faker = new Faker<SourceOrderLine>()
                .UseSeed(seed)
                .RuleFor(x => x.OrderLineId, x => x.IndexFaker + 1)
                .RuleFor(x => x.OrderId, x => x.Random.Int(1, orderIdMax))
                .RuleFor(x => x.CustomerId, x => x.Random.Int(1, customerIdMax))
                .RuleFor(x => x.ProductId, x => x.Random.Int(1, productIdMax))
                .RuleFor(x => x.Quantity, x => x.Random.Int(1, 10));

            return faker.Generate(count);
        }
    }

    [Table(nameof(OrderLine))]
    public sealed class OrderLine : Fact
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderLineKey { get; set; }
        [ForeignKey("Order")]
        public int OrderKey { get; set; }
        [ForeignKey("Customer")]
        public int CustomerKey { get; set; }
        [ForeignKey("Product")]
        public int ProductKey { get; set; }
        public int Id { get; set; }
        public int Quantity { get; set; }
        public Order Order { get; set; } = new();
        public Customer Customer { get; set; } = new();
        public Product Product { get; set; } = new();

        public static void ConfigureMapper(MapperConfiguration<SourceOrderLine, OrderLine> configuration)
        {
            configuration.Transform.ToHashCode(hash => hash.Include(x => x.Id), x => x.KeyHash);
            configuration.Transform.ToHashCode(hash => hash.All(), x => x.ChangeHash);

            configuration.Map.Property(x => x.OrderId, x => x.OrderKey);
            configuration.Map.Property(x => x.CustomerId, x => x.CustomerKey);
            configuration.Map.Property(x => x.ProductId, x => x.ProductKey);
            configuration.Map.Property(x => x.OrderLineId, x => x.Id);
            configuration.Map.Property(x => x.Quantity, x => x.Quantity);
        }
    }

    public sealed class OrderLineComparer : IEqualityComparer<IEnumerable<OrderLine>>
    {
        private readonly string[] _properties;

        public OrderLineComparer(params string[] properties)
        {
            if (properties is null || properties.Length == 0)
            {
                _properties = new[] { "OrderKey", "CustomerKey", "ProductKey", "Quantity", "KeyHash", "ChangeHash" };
            }

            _properties = properties!;
        }

        public bool Equals(IEnumerable<OrderLine>? x, IEnumerable<OrderLine>? y)
        {
            if (x is null || y is null || x.Count() != y.Count())
            {
                return false;
            }

            foreach (var item in x.Zip(y))
            {
                if (_properties.Contains("OrderKey") && item.First.OrderKey != item.Second.OrderKey) return false;
                if (_properties.Contains("CustomerKey") && item.First.CustomerKey != item.Second.CustomerKey) return false;
                if (_properties.Contains("ProductKey") && item.First.ProductKey != item.Second.ProductKey) return false;
                if (_properties.Contains("Quantity") && item.First.Quantity != item.Second.Quantity) return false;
                if (_properties.Contains("KeyHash") && item.First.KeyHash.SequenceEqual(item.Second.KeyHash)) return false;
                if (_properties.Contains("ChangeHash") && item.First.ChangeHash.SequenceEqual(item.Second.ChangeHash)) return false;
            }

            return true;
        }

        public int GetHashCode([DisallowNull] IEnumerable<OrderLine> obj)
        {
            return obj.GetHashCode();
        }
    }
}
