using Rowbot.Entities;
using Rowbot.UnitTests.Setup;
using Xunit;

namespace Rowbot.UnitTests.Entities
{
    public class EntityAccessorTests
    {
        [Fact]
        public void GetFieldMapper_Should_BuildMapperDelegate_ForValidFieldName()
        {
            var entity = new Entity<AnnotatedEntity>();
            var mapper = entity.Accessor.Value.GetFieldMapper(entity.Descriptor.Value.GetField("Id"));

            var source = new AnnotatedEntity() { Id = 1 };
            var target = new AnnotatedEntity() { Id = 0 };
            mapper(source, target);

            Assert.Equal(1, target.Id);
        }

        [Fact]
        public void GetFieldMapper_Should_GetMapperDelegateFromCache_OnSubsequentRequest()
        {
            var entity = new Entity<AnnotatedEntity>();
            var mapper1 = entity.Accessor.Value.GetFieldMapper(entity.Descriptor.Value.GetField("Id"));
            var mapper2 = entity.Accessor.Value.GetFieldMapper(entity.Descriptor.Value.GetField("Id"));

            Assert.Same(mapper1, mapper2);
        }

        [Fact]
        public void GetFieldMapperExpression_Should_BuildMapperExpression()
        {
            var property = typeof(AnnotatedEntity).GetProperty("Id");
            var field = new FieldDescriptor(property!);
            var mapperExpression = EntityAccessor<AnnotatedEntity>.BuildFieldMapperExpression(field);

            Assert.Equal("(source, target) => (target.Id = source.Id)", mapperExpression.ToString());
        }

        [Fact]
        public void GetValueMapper_Should_BuildMapperDelegate_ForValidFieldName()
        {
            var entity = new Entity<AnnotatedEntity>();
            var mapper = entity.Accessor.Value.GetValueMapper(entity.Descriptor.Value.GetField("Id"));

            var source = 1;
            var target = new AnnotatedEntity() { Id = 0 };
            mapper(source, target);

            Assert.Equal(1, target.Id);
        }

        [Fact]
        public void GetValueMapper_Should_GetMapperDelegateFromCache_OnSubsequentRequest()
        {
            var entity = new Entity<AnnotatedEntity>();
            var mapper1 = entity.Accessor.Value.GetValueMapper(entity.Descriptor.Value.GetField("Id"));
            var mapper2 = entity.Accessor.Value.GetValueMapper(entity.Descriptor.Value.GetField("Id"));

            Assert.Same(mapper1, mapper2);
        }

        [Fact]
        public void GetValueMapperExpression_Should_BuildMapperExpression()
        {
            var property = typeof(AnnotatedEntity).GetProperty("Id");
            var field = new FieldDescriptor(property!);
            var mapperExpression = EntityAccessor<AnnotatedEntity>.BuildValueMapperExpression(field);

            Assert.Equal("(source, target) => (target.Id = Convert(source, Int32))", mapperExpression.ToString());
        }

        [Fact]
        public void GetValueAccessor_Should_BuildAccessorDelegate_ForValidFieldName()
        {
            var entity = new Entity<AnnotatedEntity>();
            var valueAccessor = entity.Accessor.Value.GetValueAccessor(entity.Descriptor.Value.GetField("Id"));

            var source = new AnnotatedEntity() { Id = 1 };
            var result = valueAccessor(source);

            Assert.Equal(1, result);
        }

        [Fact]
        public void GetValueAccessor_Should_GetAccessorDelegateFromCache_OnSubsequentRequest()
        {
            var entity = new Entity<AnnotatedEntity>();
            var valueAccessor1 = entity.Accessor.Value.GetValueAccessor(entity.Descriptor.Value.GetField("Id"));
            var valueAccessor2 = entity.Accessor.Value.GetValueAccessor(entity.Descriptor.Value.GetField("Id"));

            Assert.Same(valueAccessor1, valueAccessor2);
        }

        [Fact]
        public void GetValueAccessorExpression_Should_BuildAccessorExpression()
        {
            var property = typeof(AnnotatedEntity).GetProperty("Id");
            var field = new FieldDescriptor(property!);
            var accessorExpression = EntityAccessor<AnnotatedEntity>.BuildValueAccessorExpression(field);

            Assert.Equal("x => Convert(x.Id, Object)", accessorExpression.ToString());
        }
    }
}
