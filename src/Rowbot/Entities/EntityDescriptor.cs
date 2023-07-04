using Rowbot.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;

namespace Rowbot.Entities
{
    /// <summary>
    /// Describes the entity based on the data annotations decorating the class/members
    /// </summary>
    public sealed class EntityDescriptor<TEntity>
    {
        internal readonly IList<FieldDescriptor> _fieldDescriptors;

        /// <inheritdoc cref="EntityDescriptor{TEntity}"/>
        public EntityDescriptor() : 
            this(typeof(TEntity)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => Entity.CommonDataTypes.Contains(Nullable.GetUnderlyingType(x.PropertyType) ?? x.PropertyType))) { }

        /// <inheritdoc cref="EntityDescriptor{TEntity}"/>
        public EntityDescriptor(HashSet<Type> supportedTypes) : 
            this(typeof(TEntity)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => supportedTypes.Contains(Nullable.GetUnderlyingType(x.PropertyType) ?? x.PropertyType))) { }

        /// <inheritdoc cref="EntityDescriptor{TEntity}"/>
        private EntityDescriptor(IEnumerable<PropertyInfo> properties)
        {
            var tableAttribute = typeof(TEntity).GetCustomAttribute<TableAttribute>();

            TableName = tableAttribute?.Name?.Trim('[').Trim(']') ?? typeof(TEntity).Name;
            SchemaName = tableAttribute?.Schema;

            _fieldDescriptors = properties.Select(property => new FieldDescriptor(property)).ToList();

            ConfigureKeyFields(_fieldDescriptors.Where(x => x.IsKey && x.IsMapped));
            ConfigureForeignKeyFields(_fieldDescriptors);
        }

        private EntityDescriptor(IList<FieldDescriptor> fieldDescriptors, string tableName, string? schemaName)
        {
            _fieldDescriptors = fieldDescriptors;
            TableName = tableName;
            SchemaName = schemaName;
        }

        public IEnumerable<FieldDescriptor> KeyFields => Fields.Where(x => x.IsKey).OrderBy(x => x.ColumnOrder!);
        public IEnumerable<FieldDescriptor> ForeignKeyFields => Fields.Where(x => x.IsForeignKey);
        public IEnumerable<FieldDescriptor> Fields => _fieldDescriptors.Where(x => x.IsMapped);
        public IEnumerable<FieldDescriptor> IgnoredFields => _fieldDescriptors.Where(x => !x.IsMapped);
        /// <summary>
        /// Class type name or table name if specified in <see cref="TableAttribute"/>
        /// </summary>
        public string TableName { get; private set; }
        /// <summary>
        /// Schema name if specified in <see cref="TableAttribute"/>
        /// </summary>
        public string? SchemaName { get; private set; }
        public bool HasCompositeKey => KeyFields.Count() > 1;

        /// <summary>
        /// Gets a <see cref="FieldDescriptor"/> from the collection.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="KeyNotFoundException"></exception>
        public FieldDescriptor GetField<TField>(Expression<Func<TEntity, TField>> fieldSelector)
        {
            MemberExpression memberExpression = Ensure.ArgumentIsMemberExpression(fieldSelector);
            PropertyInfo property = Ensure.MemberExpressionTargetsProperty(memberExpression);
            FieldDescriptor field = Ensure.ItemExistsInCollection(_fieldDescriptors, x => string.Equals(x.Property.Name, property.Name, StringComparison.OrdinalIgnoreCase), property.Name);

            return field;
        }

        /// <summary>
        /// Gets a <see cref="FieldDescriptor"/> from the collection.
        /// </summary>
        public FieldDescriptor GetField(string fieldOrPropertyName) =>
            Ensure.ItemExistsInCollection(_fieldDescriptors, 
                x => string.Equals(x.Property.Name, fieldOrPropertyName, StringComparison.OrdinalIgnoreCase) || string.Equals(x.Name, fieldOrPropertyName, StringComparison.OrdinalIgnoreCase), fieldOrPropertyName);

        public void UpdateField(FieldDescriptor fieldDescriptor, Action<FieldDescriptor> configure)
        {
            FieldDescriptor field = Ensure.ItemExistsInCollection(_fieldDescriptors, x => x.Name == fieldDescriptor.Name, fieldDescriptor.Name);

            configure?.Invoke(field);
        }

        internal void ConfigureKeyFields(IEnumerable<FieldDescriptor> keyFields)
        {
            if (keyFields.Count() < 2)
            {
                return;
            }

            int columnOrder = keyFields.Max(x => x.ColumnOrder ?? 0);

            foreach (var keyField in keyFields.Where(x => x.ColumnOrder is null).OrderBy(x => x.Name))
            {
                keyField.ColumnOrder = columnOrder++;
            }
        }

        internal void ConfigureForeignKeyFields(IEnumerable<FieldDescriptor> fields)
        {
            var foreignKeys = new List<(string PrincipalKey, string PrincipalEntity, string DependentKey)>();
            var properties = typeof(TEntity).GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var property in properties)
            {
                var foreignKeyAttribute = property.GetCustomAttribute<ForeignKeyAttribute>();
                if (foreignKeyAttribute is null)
                {
                    continue;
                }

                if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
                {
                    var dependentKeyProperty = properties.SingleOrDefault(x => x.Name == foreignKeyAttribute.Name);
                    if (dependentKeyProperty is null)
                    {
                        throw new InvalidOperationException($"Dependent key property with the name {foreignKeyAttribute.Name} does not exist in {typeof(TEntity).Name}");
                    }

                    var (principalKey, principalEntity) = GetPrincipalReference(property, foreignKeyAttribute.Name);

                    foreignKeys.Add((principalKey, principalEntity, dependentKeyProperty.Name));
                }
                else
                {
                    var navigationProperty = properties.SingleOrDefault(x => string.Equals(x.Name, foreignKeyAttribute.Name, StringComparison.OrdinalIgnoreCase));
                    if (navigationProperty is null)
                    {
                        throw new InvalidOperationException($"Navigation property with the name {foreignKeyAttribute.Name} does not exist in {typeof(TEntity).Name}");
                    }

                    var (principalKey, principalEntity) = GetPrincipalReference(navigationProperty, foreignKeyAttribute.Name);

                    foreignKeys.Add((principalKey, principalEntity, property.Name));
                }
            }

            foreach (var foreignKey in foreignKeys)
            {
                var fieldDescriptor = Ensure.ItemExistsInCollection(fields, x => x.Name == foreignKey.DependentKey, foreignKey.DependentKey);

                UpdateField(fieldDescriptor, field => 
                {
                    field.IsForeignKey = true;
                    field.ForeignKeyPrincipalKey = foreignKey.PrincipalKey;
                    field.ForeignKeyPrincipalName = foreignKey.PrincipalEntity;
                });
            }
        }

        private (string PrincipalKey, string PrincipalEntity) GetPrincipalReference(PropertyInfo property, string foreignKeyAttributeName)
        {
            var principalTableAttribute = property.PropertyType.GetCustomAttribute<TableAttribute>();
            if (principalTableAttribute is null)
            {
                throw new InvalidOperationException($"The class representing the principal entity of foreign key {foreignKeyAttributeName} must be decorated with a {nameof(TableAttribute)}");
            }

            var principalEntityKeys = property.PropertyType.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(x => x.GetCustomAttribute<KeyAttribute>() != null);
            if (principalEntityKeys.Count() != 1)
            {
                throw new InvalidOperationException($"The class representing the principal entity of foreign key {foreignKeyAttributeName} must have one primary key property decorated with a {nameof(KeyAttribute)}");
            }

            if (string.IsNullOrEmpty(principalTableAttribute.Schema))
            {
                return (principalEntityKeys.First().Name, $"[{principalTableAttribute.Name}]");
            }
            else
            {
                return (principalEntityKeys.First().Name, $"[{principalTableAttribute.Schema}].[{principalTableAttribute.Name}]");
            }
        }
    }
}
