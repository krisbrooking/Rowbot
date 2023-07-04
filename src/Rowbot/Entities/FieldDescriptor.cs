using Rowbot.Entities.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Rowbot.Entities
{
    /// <summary>
    /// Describes the field based on the data annotations decorating the property
    /// </summary>
    public sealed class FieldDescriptor
    {
        internal const int DEFAULT_COLUMN_LENGTH = 300;

        /// <inheritdoc cref="FieldDescriptor"/>
        public FieldDescriptor(PropertyInfo property)
        {
            var columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
            var notMappedAttribute = property.GetCustomAttribute<NotMappedAttribute>();
            var keyAttribute = property.GetCustomAttribute<KeyAttribute>();
            var requiredAttribute = property.GetCustomAttribute<RequiredAttribute>();
            var databaseGeneratedAttribute = property.GetCustomAttribute<DatabaseGeneratedAttribute>();
            var maxLengthAttribute = property.GetCustomAttribute<MaxLengthAttribute>();
            var minLengthAttribute = property.GetCustomAttribute<MinLengthAttribute>();
            var precisionAttribute = property.GetCustomAttribute<PrecisionAttribute>();

            Property = property;
            Name = columnAttribute is null || string.IsNullOrEmpty(columnAttribute.Name)
                ? property.Name
                : columnAttribute.Name.Trim('[').Trim(']');
            ColumnOrder = columnAttribute?.Order;
            DatabaseGeneratedOption = databaseGeneratedAttribute?.DatabaseGeneratedOption ?? DatabaseGeneratedOption.None;

            IsMapped = notMappedAttribute is null;
            IsKey = keyAttribute is not null;
            IsRequired = requiredAttribute is not null;
            IsTimestamp = ValidateTimestamp(property);
            IsNullable = !property.PropertyType.IsValueType || Nullable.GetUnderlyingType(property.PropertyType) is not null;

            if (property.PropertyType.IsArray || property.PropertyType == typeof(string))
            {
                MaxLength = maxLengthAttribute?.Length ?? DEFAULT_COLUMN_LENGTH;
                MinLength = minLengthAttribute?.Length ?? 0;
            }
            if (property.PropertyType == typeof(decimal) ||
                (Nullable.GetUnderlyingType(property.PropertyType) is not null && Nullable.GetUnderlyingType(property.PropertyType) == typeof(decimal)))
            {
                Precision = precisionAttribute?.Precision ?? 18;
                Scale = precisionAttribute?.Scale ?? 2;
            }
        }

        public PropertyInfo Property { get; }
        /// <summary>
        /// Property type name or column name if specified in <see cref="ColumnAttribute"/>
        /// </summary>
        public string Name { get; internal set; }
        /// <summary>
        /// Column ordinal, if specified in <see cref="ColumnAttribute"/>
        /// </summary>
        public int? ColumnOrder { get; internal set; }
        public DatabaseGeneratedOption DatabaseGeneratedOption { get; internal set; }
        /// <summary>
        /// True if property is not decorated with <see cref="NotMappedAttribute"/>
        /// </summary>
        public bool IsMapped { get; internal set; }
        /// <summary>
        /// True if property is decorated with <see cref="KeyAttribute"/>
        /// </summary>
        public bool IsKey { get; internal set; }
        /// <summary>
        /// True if property is decorated with, or referenced by a <see cref="ForeignKeyAttribute"/>
        /// </summary>
        public bool IsForeignKey { get; internal set; }
        /// <summary>
        /// The key of the principal entity pointed to by the <see cref="ForeignKeyAttribute"/>
        /// </summary>
        public string? ForeignKeyPrincipalKey { get; internal set; }
        /// <summary>
        /// The name of the principal entity pointed to by the <see cref="ForeignKeyAttribute"/>
        /// </summary>
        public string? ForeignKeyPrincipalName { get; internal set; }
        /// <summary>
        /// True if property is decorated with <see cref="UniqueAttribute"/>
        /// </summary>
        public bool IsUnique { get; internal set; }
        /// <summary>
        /// True if property is decorated with <see cref="RequiredAttribute"/>
        /// </summary>
        public bool IsRequired { get; internal set; }
        /// <summary>
        /// True if property is decorated with <see cref="TimestampAttribute"/>
        /// </summary>
        public bool IsTimestamp { get; internal set; }
        /// <summary>
        /// True if property type is <see cref="Nullable"/>
        /// </summary>
        public bool IsNullable { get; internal set; }
        /// <summary>
        /// Max size of string or array column
        /// </summary>
        public int MaxLength { get; internal set; }
        /// <summary>
        /// Min size of string or array column
        /// </summary>
        public int MinLength { get; internal set; }
        /// <summary>
        /// Precision of decimal column
        /// </summary>
        public int Precision { get; internal set; }
        /// <summary>
        /// Scale of decimal column
        /// </summary>
        public int Scale { get; internal set; }

        private bool ValidateTimestamp(PropertyInfo property)
        {
            var timestampAttribute = property.GetCustomAttribute<TimestampAttribute>();
            if (timestampAttribute is null)
            {
                return false;
            }

            if (property.PropertyType != typeof(byte[]) ||
                Nullable.GetUnderlyingType(property.PropertyType) is not null)
            {
                throw new InvalidOperationException($"{property.Name} must be non-nullable byte[] to be a Timestamp column");
            }

            return true;
        }
    }
}
