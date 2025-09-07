using Bogus;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Rowbot.Loaders.SlowlyChangingDimension;

namespace Rowbot.IntegrationTests.Setup.Entities
{
    public sealed class SourceCustomer
    {
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public bool Inactive { get; set; }
        public int Source { get; set; } = 1;

        public static IEnumerable<SourceCustomer> GetValidEntities(int count, int seed = 1)
        {
            var faker = new Faker<SourceCustomer>()
                .UseSeed(seed)
                .RuleFor(x => x.CustomerId, x => x.IndexFaker + 1)
                .RuleFor(x => x.CustomerName, x => x.Company.CompanyName())
                .RuleFor(x => x.Inactive, x => x.Random.Bool(0.1f));

            return faker.Generate(count);
        }
    }

    public sealed class SecondSourceCustomer
    {
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public bool Inactive { get; set; }
        public int Source { get; set; } = 2;

        public static IEnumerable<SecondSourceCustomer> GetValidEntities(int count, int seed = 1)
        {
            var faker = new Faker<SecondSourceCustomer>()
                .UseSeed(seed)
                .RuleFor(x => x.CustomerId, x => x.IndexFaker + 1)
                .RuleFor(x => x.CustomerName, x => x.Company.CompanyName())
                .RuleFor(x => x.Inactive, x => x.Random.Bool(0.1f));

            return faker.Generate(count);
        }
    }

    [Table(nameof(Customer))]
    public sealed class Customer : Dimension
    {
        public Customer()
        {
        }

        public Customer(int id, string? name, bool inactive, int source)
        {
            Id = id;
            IntegrationId = id;
            Name = name;
            Inactive = inactive;
            Source = source;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerKey { get; set; }
        public int Id { get; set; }
        public int IntegrationId { get; set; }
        [SlowlyChangingDimensionType2]
        [MaxLength(100)]
        public string? Name { get; set; }
        public bool Inactive { get; set; }
        public int Source { get; set; }

        public static void ConfigureMapper(MapperConfiguration<SourceCustomer, Customer> configuration)
        {
            configuration.Transform.ToHashCode(hash => hash.WithSeed(1).Include(x => x.Id), x => x.KeyHash);
            configuration.Transform.ToHashCode(hash => hash.WithSeed(1).All(), x => x.ChangeHash);

            configuration.Map.Property(x => x.CustomerId, x => x.Id);
            configuration.Map.Property(x => x.CustomerId, x => x.IntegrationId);
            configuration.Map.Property(x => x.CustomerName, x => x.Name);
            configuration.Map.Property(x => x.Inactive, x => x.Inactive);
            configuration.Map.Property(x => x.Source, x => x.Source);
        }

        public static void ConfigureMapper(MapperConfiguration<SecondSourceCustomer, Customer> configuration)
        {
            configuration.Transform.ToHashCode(hash => hash.WithSeed(2).Include(x => x.Id), x => x.KeyHash);
            configuration.Transform.ToHashCode(hash => hash.WithSeed(2).All(), x => x.ChangeHash);

            configuration.Map.Property(x => x.CustomerId, x => x.Id);
            configuration.Map.Property(x => x.CustomerId, x => x.IntegrationId);
            configuration.Map.Property(x => x.CustomerName, x => x.Name);
            configuration.Map.Property(x => x.Inactive, x => x.Inactive);
            configuration.Map.Property(x => x.Source, x => x.Source);
        }
    }

    public sealed class CustomerComparer : IEqualityComparer<Customer>
    {
        private readonly string[] _properties;

        public CustomerComparer(params string[] properties)
        {
            if (properties is null || properties.Length == 0)
            {
                _properties = new[] { "Id", "Name", "Inactive", "IsActive", "KeyHash", "ChangeHash" };
            }

            _properties = properties!;
        }

        public bool Equals(Customer? x, Customer? y)
        {
            if (x is null || y is null)
            {
                return false;
            }

            if (_properties.Contains("Id") && x.Id != y.Id) return false;
            if (_properties.Contains("Name") && x.Name != y.Name) return false;
            if (_properties.Contains("Inactive") && x.Inactive != y.Inactive) return false;
            if (_properties.Contains("IsActive") && x.IsActive != y.IsActive) return false;
            if (_properties.Contains("KeyHash") && x.KeyHash.SequenceEqual(y.KeyHash)) return false;
            if (_properties.Contains("ChangeHash") && x.ChangeHash.SequenceEqual(y.ChangeHash)) return false;

            return true;
        }

        public int GetHashCode([DisallowNull] Customer obj)
        {
            return obj.GetHashCode();
        }
    }

    public sealed class CustomersComparer : IEqualityComparer<IEnumerable<Customer>>
    {
        private readonly string[] _properties;

        public CustomersComparer(params string[] properties)
        {
            if (properties is null || properties.Length == 0)
            {
                _properties = new[] { "Id", "Name", "Inactive", "IsActive", "KeyHash", "ChangeHash" };
            }

            _properties = properties!;
        }

        public bool Equals(IEnumerable<Customer>? x, IEnumerable<Customer>? y)
        {
            if (x is null || y is null || x.Count() != y.Count())
            {
                return false;
            }

            foreach (var item in x.Zip(y))
            {
                if (_properties.Contains("Id") && item.First.Id != item.Second.Id) return false;
                if (_properties.Contains("Name") && item.First.Name != item.Second.Name) return false;
                if (_properties.Contains("Inactive") && item.First.Inactive != item.Second.Inactive) return false;
                if (_properties.Contains("IsActive") && item.First.IsActive != item.Second.IsActive) return false;
                if (_properties.Contains("KeyHash") && item.First.KeyHash.SequenceEqual(item.Second.KeyHash)) return false;
                if (_properties.Contains("ChangeHash") && item.First.ChangeHash.SequenceEqual(item.Second.ChangeHash)) return false;
            }

            return true;
        }

        public int GetHashCode([DisallowNull] IEnumerable<Customer> obj)
        {
            return obj.GetHashCode();
        }
    }
}
