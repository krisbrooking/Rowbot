using Rowbot.Entities;
using Rowbot.UnitTests.Setup;
using System;
using Xunit;

namespace Rowbot.UnitTests.Entities
{
    public class FieldDescriptorTests
    {
        [Fact]
        public void FieldDescriptor_Should_SetNameToPropertyName_WhenThereIsNoColumnAttribute()
        {
            var property = typeof(AnnotatedEntity).GetProperty("Id");
            var field = new FieldDescriptor(property!);
            Assert.Equal("Id", field.Name);
        }

        [Fact]
        public void FieldDescriptor_Should_SetIsKeyToTrue_ForKeyAttribute()
        {
            var property = typeof(AnnotatedEntity).GetProperty("Id");
            var field = new FieldDescriptor(property!);
            Assert.True(field.IsKey);
        }

        [Fact]
        public void FieldDescriptor_Should_SetNameToColumnName_WhenColumnAttributeIncludesName()
        {
            var property = typeof(AnnotatedEntity).GetProperty("Name");
            var field = new FieldDescriptor(property!);
            Assert.Equal("ColumnName", field.Name);
        }

        [Fact]
        public void FieldDescriptor_Should_OverrideDefaultMaxLength()
        {
            var property = typeof(AnnotatedEntity).GetProperty("Description");
            var field = new FieldDescriptor(property!);
            Assert.Equal(100, field.MaxLength);
        }

        [Fact]
        public void FieldDescriptor_Should_SetIsTimeStampToTrue_WhenTimetampFieldIsByteArray()
        {
            var property = typeof(AnnotatedEntityWithValidTimestamp).GetProperty("ValidTimestamp");
            var field = new FieldDescriptor(property!);
            Assert.True(field.IsTimestamp);
        }

        [Fact]
        public void FieldDescriptor_Should_ThrowInvalidOperationException_WhenTimetampFieldIsNotByteArray()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var property = typeof(AnnotatedEntityWithInvalidTimestamp).GetProperty("InvalidTimestamp");
                var field = new FieldDescriptor(property!);
            });
        }
    }
}
