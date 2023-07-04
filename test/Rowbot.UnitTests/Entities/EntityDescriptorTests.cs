using Rowbot.Entities;
using Rowbot.UnitTests.Setup;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Xunit;

namespace Rowbot.UnitTests.Entities
{
    public class EntityDescriptorTests
    {
        [Fact]
        public void EntityDescriptor_Should_SetTableNameToClassName_WhenTableAttributeDoesNotExist()
        {
            var entity = new EntityDescriptor<SourcePerson>();
            Assert.Equal("SourcePerson", entity.TableName);
        }

        [Fact]
        public void EntityDescriptor_Should_OverrideClassName_WhenTableAttributeExists()
        {
            var entity = new EntityDescriptor<AnnotatedEntity>();
            Assert.Equal("AnnotatedEntities", entity.TableName);
        }

        [Fact]
        public void EntityDescriptor_Should_CreateFieldDescriptors_ForPublicProperties()
        {
            var entity = new EntityDescriptor<AnnotatedEntity>();
            Assert.Equal(3, entity.Fields.Count());
        }

        [Fact]
        public void EntityDescriptor_Should_CreateFieldDescriptors_ForPublicPropertiesOfDerivedClass()
        {
            var entity = new EntityDescriptor<AnnotatedEntityWithValidTimestamp>();
            Assert.Equal(4, entity.Fields.Count());
        }

        [Fact]
        public void EntityDescriptor_Should_IgnoreNonPublicProperties()
        {
            var entity = new EntityDescriptor<AnnotatedEntityWithNonPublicProperties>();
            Assert.Equal(3, entity.Fields.Count());
        }

        [Fact]
        public void EntityDescriptor_Should_IgnorePublicFields()
        {
            var entity = new EntityDescriptor<AnnotatedEntityWithPublicFields>();
            Assert.Equal(3, entity.Fields.Count());
        }

        [Fact]
        public void EntityDescriptor_Should_SetSchemaName_WhenTableAttributeHasSchema()
        {
            var entity = new EntityDescriptor<AnnotatedEntity>();
            Assert.Equal("dbo", entity.SchemaName);
        }

        [Fact]
        public void EntityDescriptor_Should_CreateCompositeKeyList_ForCompositeKeyWithColumnOrdering()
        {
            var entity = new EntityDescriptor<AnnotatedEntityWithCompositeKeyColumnOrdering>();
            Assert.Equal(new List<string> { "Name", "Id" }, entity.KeyFields.Select(x => x.Property.Name));
        }

        [Fact]
        public void EntityDescriptor_Should_OrderCompositeKeyByName_ForCompositeKeyWithNoColumnOrdering()
        {
            var entity = new EntityDescriptor<AnnotatedEntityWithCompositeKeyNoColumnOrdering>();
            Assert.Equal(new List<string> { "Id", "Name" }, entity.KeyFields.Select(x => x.Property.Name));
        }

        [Fact]
        public void EntityDescriptor_Should_CreateForeignKeyField_WhenForeignKeyAttributeOnLocalProperty()
        {
            var entity = new EntityDescriptor<EntityWithForeignKeyOnLocalProperty>();
            Assert.Single(entity.ForeignKeyFields);
        }

        [Fact]
        public void EntityDescriptor_Should_ConfigureCorrectForeignKeyField_WhenForeignKeyAttributeOnLocalProperty()
        {
            var entity = new EntityDescriptor<EntityWithForeignKeyOnLocalProperty>();
            Assert.True(entity.Fields.Single(x => x.Name == "RelatedTableId").IsForeignKey);
        }

        [Fact]
        public void EntityDescriptor_Should_SetPrincipalOnForeignKeyField_WhenForeignKeyAttributeOnLocalProperty()
        {
            var entity = new EntityDescriptor<EntityWithForeignKeyOnLocalProperty>();
            Assert.Equal("RelatedTableId", entity.ForeignKeyFields.First(x => x.Name == "RelatedTableId").ForeignKeyPrincipalKey);
            Assert.Equal("[RelatedTable]", entity.ForeignKeyFields.First(x => x.Name == "RelatedTableId").ForeignKeyPrincipalName);
        }

        [Fact]
        public void EntityDescriptor_Should_ThrowInvalidOperationException_WhenForeignKeyAttributeOnLocalPropertyAndThereIsNoNavigationProperty()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var entity = new EntityDescriptor<EntityWithForeignKeyOnLocalPropertyNoLocalNavigationProperty>();
            });
        }

        [Fact]
        public void EntityDescriptor_Should_CreateForeignKeyField_WhenForeignKeyAttributeOnNavigationProperty()
        {
            var entity = new EntityDescriptor<EntityWithForeignKeyOnLocalNavigationProperty>();
            Assert.Single(entity.ForeignKeyFields);
        }

        [Fact]
        public void EntityDescriptor_Should_ConfigureCorrectForeignKeyField_WhenForeignKeyAttributeOnNavigationProperty()
        {
            var entity = new EntityDescriptor<EntityWithForeignKeyOnLocalNavigationProperty>();
            Assert.True(entity.Fields.Single(x => x.Name == "RelatedTableId").IsForeignKey);
        }

        [Fact]
        public void EntityDescriptor_Should_SetPrincipalOnForeignKeyField_WhenForeignKeyAttributeOnNavigationProperty()
        {
            var entity = new EntityDescriptor<EntityWithForeignKeyOnLocalNavigationProperty>();
            Assert.Equal("RelatedTableId", entity.ForeignKeyFields.First(x => x.Name == "RelatedTableId").ForeignKeyPrincipalKey);
            Assert.Equal("[RelatedTable]", entity.ForeignKeyFields.First(x => x.Name == "RelatedTableId").ForeignKeyPrincipalName);
        }

        [Fact]
        public void EntityDescriptor_Should_ThrowInvalidOperationException_WhenForeignKeyAttributeOnNavigationPropertyButThereIsNoLocalProperty()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var entity = new EntityDescriptor<EntityWithForeignKeyOnLocalNavigationPropertyNoLocalProperty>();
            });
        }

        [Fact]
        public void EntityDescriptor_Should_ThrowInvalidOperationException_WhenForeignKeyAttributePrincipalEntityHasNoTableAttribute()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var entity = new EntityDescriptor<EntityWithForeignKeyOnLocalPropertyPrincipalEntityHasNoTableAttribute>();
            });
        }

        [Fact]
        public void EntityDescriptor_Should_ThrowInvalidOperationException_WhenForeignKeyAttributePrincipalEntityHasNoPrimaryKey()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var entity = new EntityDescriptor<EntityWithForeignKeyOnLocalPropertyPrincipalEntityHasNoPrimaryKey>();
            });
        }

        [Fact]
        public void GetField_Should_ReturnFieldDescriptor()
        {
            var entity = new EntityDescriptor<AnnotatedEntity>();
            var field = entity.GetField(x => x.Name);
            Assert.Equal("ColumnName", field.Name);
        }

        [Fact]
        public void GetField_Should_ThrowArgumentException_WhenFieldSelectorIsSelectingPublicField()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var entity = new EntityDescriptor<AnnotatedEntityWithPublicFields>();
                var field = entity.GetField(x => x.PublicField);
            });
        }

        [Fact]
        public void GetField_Should_ThrowKeyNotFoundException_WhenFieldSelectorIsSelectingInternalProperty()
        {
            Assert.Throws<KeyNotFoundException>(() =>
            {
                var entity = new EntityDescriptor<AnnotatedEntityWithNonPublicProperties>();
                var field = entity.GetField(x => x.InternalProperty);
            });
        }

        [Fact]
        public void UpdateField_Should_UpdateFieldDescriptor()
        {
            var entity = new EntityDescriptor<AnnotatedEntity>();
            entity.UpdateField(entity.Fields.Single(x => x.Name == "Id"), field => field.IsKey = !field.IsKey);

            Assert.False(entity.Fields.Single(x => x.Name == "Id").IsKey);
        }

        #region ForeignKeyEntities
        public class EntityWithForeignKeyOnLocalProperty
        {
            [ForeignKey("RelatedTable")]
            public int RelatedTableId { get; set; }
            public RelatedTable RelatedTable { get; set; } = new();
        }

        public class EntityWithForeignKeyOnLocalNavigationProperty
        {
            public int RelatedTableId { get; set; }
            [ForeignKey("RelatedTableId")]
            public RelatedTable RelatedTable { get; set; } = new();
        }

        public class EntityWithForeignKeyOnLocalNavigationPropertyNoLocalProperty
        {
            [ForeignKey("RelatedTableId")]
            public RelatedTable RelatedTable { get; set; } = new();
        }

        public class EntityWithForeignKeyOnLocalPropertyNoLocalNavigationProperty
        {
            [ForeignKey("RelatedTable")]
            public int RelatedTableId { get; set; }
        }

        [Table("RelatedTable")]
        public class RelatedTable
        {
            [Key]
            public int RelatedTableId { get; set; }
        }

        public class EntityWithForeignKeyOnLocalPropertyPrincipalEntityHasNoTableAttribute
        {
            [ForeignKey("RelatedTable")]
            public int RelatedTableId { get; set; }
            public RelatedTableNoTableAttribute RelatedTable { get; set; } = new();
        }

        public class RelatedTableNoTableAttribute
        {
            [Key]
            public int RelatedTableId { get; set; }
        }

        public class EntityWithForeignKeyOnLocalPropertyPrincipalEntityHasNoPrimaryKey
        {
            [ForeignKey("RelatedTable")]
            public int RelatedTableId { get; set; }
            public RelatedTableNoPrimaryKey RelatedTable { get; set; } = new();
        }

        [Table("RelatedTable")]
        public class RelatedTableNoPrimaryKey
        {
            public int RelatedTableId { get; set; }
        }
        #endregion
    }
}
