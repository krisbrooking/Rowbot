using Microsoft.Data.SqlClient;
using Rowbot.Entities;
using Rowbot.Framework.Blocks.Connectors.Database;
using System.Data;

namespace Rowbot.Connectors.SqlServer
{
    public class SqlServerCommandProvider : IDbCommandProvider
    {
        public IDbCommand CreateCommand(string commandText) => new SqlCommand(commandText);

        public IDbCommand AddOrUpdateParameter(IDbCommand command, ExtractParameter extractParameter)
        {
            if (command.Parameters is SqlParameterCollection parameterCollection)
            {
                var parameterName = $"@{extractParameter.ParameterName.Trim('@')}";
                if (parameterCollection.Contains(parameterName))
                {
                    var index = parameterCollection.IndexOf(parameterName);
                    parameterCollection[index].Value = extractParameter.ParameterValue ?? DBNull.Value;
                    parameterCollection[index].IsNullable = extractParameter.IsNullable;
                }
                else
                {
                    var parameter = parameterCollection.Add(new SqlParameter(parameterName, extractParameter.ParameterValue ?? DBNull.Value));
                    parameter.SqlDbType = GetSqlType(extractParameter.ParameterType);
                    parameter.IsNullable = extractParameter.IsNullable;
                }
            }

            return command;
        }

        public IDbCommand AddParameters(IDbCommand command, IEnumerable<ExtractParameter> parameters)
        {
            foreach (var parameter in parameters)
            {
                AddOrUpdateParameter(command, parameter);
            }

            return command;
        }

        public string GetDatabaseType(FieldDescriptor field) => field switch
        {
            FieldDescriptor _ when GetSqlType(field.Property.PropertyType) == SqlDbType.BigInt => "BIGINT",
            FieldDescriptor _ when GetSqlType(field.Property.PropertyType) == SqlDbType.Binary => field.MaxLength > 0 ? $"VARBINARY({field.MaxLength})" : "VARBINARY",
            FieldDescriptor _ when GetSqlType(field.Property.PropertyType) == SqlDbType.Bit => "BIT",
            FieldDescriptor _ when GetSqlType(field.Property.PropertyType) == SqlDbType.Char => "CHAR",
            FieldDescriptor _ when GetSqlType(field.Property.PropertyType) == SqlDbType.Date => "DATE",
            FieldDescriptor _ when GetSqlType(field.Property.PropertyType) == SqlDbType.DateTime => "DATETIME",
            FieldDescriptor _ when GetSqlType(field.Property.PropertyType) == SqlDbType.DateTime2 => field.MaxLength > 0 ? $"DATETIME2({field.MaxLength})" : "DATETIME2",
            FieldDescriptor _ when GetSqlType(field.Property.PropertyType) == SqlDbType.DateTimeOffset => field.MaxLength > 0 ? $"DATETIMEOFFSET({field.MaxLength})" : "DATETIMEOFFSET",
            FieldDescriptor _ when GetSqlType(field.Property.PropertyType) == SqlDbType.Decimal => "DECIMAL(18, 2)",
            FieldDescriptor _ when GetSqlType(field.Property.PropertyType) == SqlDbType.Float => field.MaxLength > 0 ? $"FLOAT({field.MaxLength})" : "FLOAT",
            FieldDescriptor _ when GetSqlType(field.Property.PropertyType) == SqlDbType.Int => "INT",
            FieldDescriptor _ when GetSqlType(field.Property.PropertyType) == SqlDbType.NChar => field.MaxLength > 0 ? $"NCHAR({field.MaxLength})" : "NCHAR",
            FieldDescriptor _ when GetSqlType(field.Property.PropertyType) == SqlDbType.NVarChar => field.MaxLength > 0 ? $"NVARCHAR({field.MaxLength})" : "NVARCHAR",
            FieldDescriptor _ when GetSqlType(field.Property.PropertyType) == SqlDbType.Real => "FLOAT",
            FieldDescriptor _ when GetSqlType(field.Property.PropertyType) == SqlDbType.SmallDateTime => "SMALLDATETIME",
            FieldDescriptor _ when GetSqlType(field.Property.PropertyType) == SqlDbType.SmallInt => "SMALLINT",
            FieldDescriptor _ when GetSqlType(field.Property.PropertyType) == SqlDbType.Time => field.MaxLength > 0 ? $"TIME({field.MaxLength})" : "TIME",
            FieldDescriptor _ when GetSqlType(field.Property.PropertyType) == SqlDbType.TinyInt => "TINYINT",
            FieldDescriptor _ when GetSqlType(field.Property.PropertyType) == SqlDbType.UniqueIdentifier => "UNIQUEIDENTIFIER",
            FieldDescriptor _ when GetSqlType(field.Property.PropertyType) == SqlDbType.VarBinary => field.MaxLength > 0 ? $"VARBINARY({field.MaxLength})" : "VARBINARY",
            FieldDescriptor _ when GetSqlType(field.Property.PropertyType) == SqlDbType.VarChar => field.MaxLength > 0 ? $"NCHAR({field.MaxLength})" : "NCHAR",
            _ => throw new ArgumentException($"Unsupported data type {field.Property.PropertyType.Name}")
        };

        private SqlDbType GetSqlType(Type propertyType) => propertyType switch
        {
            Type _ when Nullable.GetUnderlyingType(propertyType) is not null => GetSqlType(Nullable.GetUnderlyingType(propertyType)!),
            Type _ when propertyType == typeof(bool) => SqlDbType.Bit,
            Type _ when propertyType == typeof(byte) => SqlDbType.TinyInt,
            Type _ when propertyType == typeof(byte[]) => SqlDbType.VarBinary,
            Type _ when propertyType == typeof(char) => SqlDbType.Char,
            Type _ when propertyType == typeof(char[]) => SqlDbType.NVarChar,
            Type _ when propertyType == typeof(DateOnly) => SqlDbType.Date,
            Type _ when propertyType == typeof(DateTime) => SqlDbType.DateTime,
            Type _ when propertyType == typeof(DateTimeOffset) => SqlDbType.DateTimeOffset,
            Type _ when propertyType == typeof(decimal) => SqlDbType.Decimal,
            Type _ when propertyType == typeof(double) => SqlDbType.Float,
            Type _ when propertyType == typeof(Guid) => SqlDbType.UniqueIdentifier,
            Type _ when propertyType == typeof(short) => SqlDbType.SmallInt,
            Type _ when propertyType == typeof(int) => SqlDbType.Int,
            Type _ when propertyType == typeof(long) => SqlDbType.BigInt,
            Type _ when propertyType == typeof(sbyte) => SqlDbType.TinyInt,
            Type _ when propertyType == typeof(float) => SqlDbType.Real,
            Type _ when propertyType == typeof(string) => SqlDbType.NVarChar,
            Type _ when propertyType == typeof(TimeOnly) => SqlDbType.Time,
            Type _ when propertyType == typeof(TimeSpan) => SqlDbType.Time,
            Type _ when propertyType == typeof(ushort) => SqlDbType.SmallInt,
            Type _ when propertyType == typeof(uint) => SqlDbType.Int,
            Type _ when propertyType == typeof(ulong) => SqlDbType.BigInt,
            _ => SqlDbType.NVarChar
        };
    }
}
