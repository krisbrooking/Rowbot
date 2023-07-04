using Bogus;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Rowbot.IntegrationTests.Setup.Entities
{
    public sealed class SourceOrder
    {
        public int OrderId { get; set; }
        public DateTime? OrderDate { get; set; }

        public static IEnumerable<SourceOrder> GetValidEntities(int count, int seed = 1)
        {
            var faker = new Faker<SourceOrder>()
                .UseSeed(seed)
                .RuleFor(x => x.OrderId, x => x.IndexFaker + 1)
                .RuleFor(x => x.OrderDate, x => x.Date.Between(new DateTime(2023, 01, 01), new DateTime(2023, 12, 31)));

            return faker.Generate(count);
        }
    }

    [Table(nameof(Order))]
    public sealed class Order : Dimension
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderKey { get; set; }
        public int Id { get; set; }
        public DateTime? OrderDate { get; set; }
        public decimal? SubTotal { get; set; }
        public Customer? Customer { get; set; }

        public static void ConfigureMapper(MapperConfiguration<SourceOrder, Order> configuration)
        {
            configuration.Transform.ToHashCode(hash => hash.Include(x => x.Id), x => x.KeyHash);
            configuration.Transform.ToHashCode(hash => hash.All(), x => x.ChangeHash);

            configuration.Map.Property(x => x.OrderId, x => x.OrderKey);
            configuration.Map.Property(x => x.OrderId, x => x.Id);
            configuration.Map.Property(x => x.OrderDate, x => x.OrderDate);
        }
    }

    public sealed class OrderComparer : IEqualityComparer<IEnumerable<Order>>
    {
        private readonly string[] _properties;

        public OrderComparer(params string[] properties)
        {
            if (properties is null || properties.Length == 0)
            {
                _properties = new[] { "Id", "OrderDate", "IsActive", "KeyHash", "ChangeHash" };
            }

            _properties = properties!;
        }

        public bool Equals(IEnumerable<Order>? x, IEnumerable<Order>? y)
        {
            if (x is null || y is null || x.Count() != y.Count())
            {
                return false;
            }

            foreach (var item in x.Zip(y))
            {
                if (_properties.Contains("Id") && item.First.Id != item.Second.Id) return false;
                if (_properties.Contains("OrderDate") && item.First.OrderDate != item.Second.OrderDate) return false;
                if (_properties.Contains("SubTotal") && item.First.SubTotal != item.Second.SubTotal) return false;
                if (_properties.Contains("IsActive") && item.First.IsActive != item.Second.IsActive) return false;
                if (_properties.Contains("KeyHash") && item.First.KeyHash.SequenceEqual(item.Second.KeyHash)) return false;
                if (_properties.Contains("ChangeHash") && item.First.ChangeHash.SequenceEqual(item.Second.ChangeHash)) return false;
            }

            return true;
        }

        public int GetHashCode([DisallowNull] IEnumerable<Order> obj)
        {
            return obj.GetHashCode();
        }
    }
}
