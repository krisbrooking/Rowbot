using Rowbot.Entities;
using System;
using System.Collections.Generic;
using Xunit;

namespace Rowbot.UnitTests.Entities
{
    public class EntityComparerTests
    {
        [Fact]
        public void FieldEquals_Should_CreatePredicateThatReturnsTrueWhenValuesAreEqual()
        {
            var leftEntity = ComparerEntity.GetValidEntity();
            var rightEntity = new ComparerEntity { Primitive = "Primitive" };

            var entityDescriptor = new EntityDescriptor<ComparerEntity>();
            var comparer = new EntityComparer<ComparerEntity>(entityDescriptor);

            Assert.True(comparer.FieldEquals(entityDescriptor.GetField(x => x.Primitive), leftEntity, rightEntity));
        }

        [Fact]
        public void FieldEquals_Should_CreatePredicateThatReturnsFalseWhenValuesNotEqual()
        {
            var leftEntity = ComparerEntity.GetValidEntity();
            var rightEntity = new ComparerEntity { Primitive = "NonMatchingValue" };

            var entityDescriptor = new EntityDescriptor<ComparerEntity>();
            var comparer = new EntityComparer<ComparerEntity>(entityDescriptor);

            Assert.False(comparer.FieldEquals(entityDescriptor.GetField(x => x.Primitive), leftEntity, rightEntity));
        }

        [Fact]
        public void FieldEquals_Should_CreatePredicateThatReturnsTrueWhenArraysAreEqual()
        {
            var leftEntity = ComparerEntity.GetValidEntity();
            var rightEntity = new ComparerEntity { Array = new byte[] { 1, 2, 3 } };

            var entityDescriptor = new EntityDescriptor<ComparerEntity>();
            var comparer = new EntityComparer<ComparerEntity>(entityDescriptor);

            Assert.True(comparer.FieldEquals(entityDescriptor.GetField(x => x.Array), leftEntity, rightEntity));
        }

        [Fact]
        public void FieldEquals_Should_CreatePredicateThatReturnsFalseWhenArraysAreNotEqual()
        {
            var leftEntity = ComparerEntity.GetValidEntity();
            var rightEntity = new ComparerEntity { Array = new byte[] { 1, 2, 3, 4 } };

            var entityDescriptor = new EntityDescriptor<ComparerEntity>();
            var comparer = new EntityComparer<ComparerEntity>(entityDescriptor);

            Assert.False(comparer.FieldEquals(entityDescriptor.GetField(x => x.Array), leftEntity, rightEntity));
        }

        [Fact]
        public void FieldEquals_Should_CreatePredicateThatReturnsTrueWhenDateTimesAreEqual()
        {
            var leftEntity = ComparerEntity.GetValidEntity();
            var rightEntity = new ComparerEntity { Date = new DateTime(1970, 1, 1) };

            var entityDescriptor = new EntityDescriptor<ComparerEntity>();
            var comparer = new EntityComparer<ComparerEntity>(entityDescriptor);

            Assert.True(comparer.FieldEquals(entityDescriptor.GetField(x => x.Date), leftEntity, rightEntity));
        }

        [Fact]
        public void FieldEquals_Should_CreatePredicateThatReturnsFalseWhenDateTimesAreNotEqual()
        {
            var leftEntity = ComparerEntity.GetValidEntity();
            var rightEntity = new ComparerEntity { Date = new DateTime(1900, 1, 1) };

            var entityDescriptor = new EntityDescriptor<ComparerEntity>();
            var comparer = new EntityComparer<ComparerEntity>(entityDescriptor);

            Assert.False(comparer.FieldEquals(entityDescriptor.GetField(x => x.Date), leftEntity, rightEntity));
        }

        [Fact]
        public void EntityEquals_Should_ReturnIsEqual()
        {
            var leftEntity = ComparerEntity.GetValidEntity();
            var rightEntity = ComparerEntity.GetValidEntity();

            var comparer = new EntityComparer<ComparerEntity>(new EntityDescriptor<ComparerEntity>());

            Assert.True(comparer.EntityEquals(leftEntity, rightEntity).IsEqual);
        }

        [Fact]
        public void EntityEquals_Should_ReturnNotEqual()
        {
            var leftEntity = ComparerEntity.GetValidEntity();
            var rightEntity = new ComparerEntity();

            var comparer = new EntityComparer<ComparerEntity>(new EntityDescriptor<ComparerEntity>());

            Assert.False(comparer.EntityEquals(leftEntity, rightEntity).IsEqual);
        }

        [Fact]
        public void EntityEquals_ShouldReturnListOfPropertiesWithDifferingValues()
        {
            var leftEntity = new ComparerEntity { Primitive = "A", Array = new byte[] { 1, 2, 3 }, Date = new DateTime(1970, 1, 1) };
            var rightEntity = new ComparerEntity { Primitive = "B", Array = new byte[] { 1, 2, 3 }, Date = new DateTime(1900, 1, 1) };

            var comparer = new EntityComparer<ComparerEntity>(new EntityDescriptor<ComparerEntity>());

            Assert.Equal(new List<string> { "Primitive", "Date" }, comparer.EntityEquals(leftEntity, rightEntity).DifferentPropertyValues);
        }

        class ComparerEntity
        {
            public string? Primitive { get; set; }
            public byte[]? Array { get; set; }
            public DateTime Date { get; set; }

            public static ComparerEntity GetValidEntity() => new ComparerEntity { Primitive = "Primitive", Array = new byte[] { 1, 2, 3 }, Date = new DateTime(1970, 1, 1) };
        }
    }
}
