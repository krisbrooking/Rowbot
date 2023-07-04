using Bogus;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Rowbot.IntegrationTests.Setup.Entities
{
    public sealed class SourceProduct
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public decimal? Price { get; set; }
        public decimal? Cost { get; set; }

        public static IEnumerable<SourceProduct> GetValidEntities(int count, int seed = 1)
        {
            var faker = new Faker<SourceProduct>()
                .UseSeed(seed)
                .RuleFor(x => x.ProductId, x => x.IndexFaker + 1)
                .RuleFor(x => x.ProductName, x => x.Commerce.ProductName())
                .RuleFor(x => x.Price, x => x.Random.Decimal(40, 50))
                .RuleFor(x => x.Cost, x => x.Random.Decimal(30, 40));

            return faker.Generate(count);
        }
    }

    [Table(nameof(Product))]
    public sealed class Product : Dimension
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProductKey { get; set; }
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? UnitCost { get; set; }

        public static void ConfigureMapper(MapperConfiguration<SourceProduct, Product> configuration)
        {
            configuration.Transform.ToHashCode(hash => hash.Include(x => x.Id), x => x.KeyHash);
            configuration.Transform.ToHashCode(hash => hash.All(), x => x.ChangeHash);

            configuration.Map.Property(x => x.ProductId, x => x.Id);
            configuration.Map.Property(x => x.ProductName, x => x.Name);
            configuration.Map.Property(x => x.Price, x => x.UnitPrice);
            configuration.Map.Property(x => x.Cost, x => x.UnitCost);
        }
    }

    public sealed class ProductComparer : IEqualityComparer<IEnumerable<Product>>
    {
        private readonly string[] _properties;

        public ProductComparer(params string[] properties)
        {
            if (properties is null || properties.Length == 0)
            {
                _properties = new[] { "Id", "Name", "UnitPrice", "UnitCost", "IsActive", "KeyHash", "ChangeHash" };
            }

            _properties = properties!;
        }

        public bool Equals(IEnumerable<Product>? x, IEnumerable<Product>? y)
        {
            if (x is null || y is null || x.Count() != y.Count())
            {
                return false;
            }

            foreach (var item in x.Zip(y))
            {
                if (_properties.Contains("Id") && item.First.Id != item.Second.Id) return false;
                if (_properties.Contains("Name") && item.First.Name != item.Second.Name) return false;
                if (_properties.Contains("UnitPrice") && item.First.UnitPrice != item.Second.UnitPrice) return false;
                if (_properties.Contains("UnitCost") && item.First.UnitCost != item.Second.UnitCost) return false;
                if (_properties.Contains("IsActive") && item.First.IsActive != item.Second.IsActive) return false;
                if (_properties.Contains("KeyHash") && item.First.KeyHash.SequenceEqual(item.Second.KeyHash)) return false;
                if (_properties.Contains("ChangeHash") && item.First.ChangeHash.SequenceEqual(item.Second.ChangeHash)) return false;
            }

            return true;
        }

        public int GetHashCode([DisallowNull] IEnumerable<Product> obj)
        {
            return obj.GetHashCode();
        }
    }
}
