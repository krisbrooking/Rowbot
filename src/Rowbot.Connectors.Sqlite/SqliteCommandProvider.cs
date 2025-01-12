using Microsoft.Data.Sqlite;
using Rowbot.Entities;
using Rowbot.Connectors.Common.Database;
using System.Data;

namespace Rowbot.Connectors.Sqlite
{
    public sealed class SqliteCommandProvider : IDbCommandProvider
    {
        public IDbCommand CreateCommand(string commandText) => new SqliteCommand(commandText);

        public IDbCommand AddOrUpdateParameter(IDbCommand command, ExtractParameter extractParameter)
        {
            if (command.Parameters is SqliteParameterCollection parameterCollection)
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
                    var parameter = parameterCollection.Add(new SqliteParameter(parameterName, extractParameter.ParameterValue ?? DBNull.Value));
                    parameter.SqliteType = GetSqliteType(extractParameter.ParameterType);
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
            FieldDescriptor _ when GetSqliteType(field.Property.PropertyType) == SqliteType.Integer => "INTEGER",
            FieldDescriptor _ when GetSqliteType(field.Property.PropertyType) == SqliteType.Real => "REAL",
            FieldDescriptor _ when GetSqliteType(field.Property.PropertyType) == SqliteType.Blob => "BLOB",
            _ => field.MaxLength > 0 ? $"TEXT CHECK(length({field.Name}) <= {field.MaxLength})" : "TEXT"
        };

        private SqliteType GetSqliteType(Type propertyType) => propertyType switch
        {
            Type _ when Nullable.GetUnderlyingType(propertyType) is not null => GetSqliteType(Nullable.GetUnderlyingType(propertyType)!),
            Type _ when propertyType == typeof(bool) => SqliteType.Integer,
            Type _ when propertyType == typeof(byte) => SqliteType.Integer,
            Type _ when propertyType == typeof(byte[]) => SqliteType.Blob,
            Type _ when propertyType == typeof(char) => SqliteType.Text,
            Type _ when propertyType == typeof(char[]) => SqliteType.Blob,
            Type _ when propertyType == typeof(DateOnly) => SqliteType.Text,
            Type _ when propertyType == typeof(DateTime) => SqliteType.Text,
            Type _ when propertyType == typeof(DateTimeOffset) => SqliteType.Text,
            Type _ when propertyType == typeof(decimal) => SqliteType.Real,
            Type _ when propertyType == typeof(double) => SqliteType.Real,
            Type _ when propertyType == typeof(Guid) => SqliteType.Text,
            Type _ when propertyType == typeof(short) => SqliteType.Integer,
            Type _ when propertyType == typeof(int) => SqliteType.Integer,
            Type _ when propertyType == typeof(long) => SqliteType.Integer,
            Type _ when propertyType == typeof(sbyte) => SqliteType.Integer,
            Type _ when propertyType == typeof(float) => SqliteType.Real,
            Type _ when propertyType == typeof(string) => SqliteType.Text,
            Type _ when propertyType == typeof(TimeOnly) => SqliteType.Text,
            Type _ when propertyType == typeof(TimeSpan) => SqliteType.Text,
            Type _ when propertyType == typeof(ushort) => SqliteType.Integer,
            Type _ when propertyType == typeof(uint) => SqliteType.Integer,
            Type _ when propertyType == typeof(ulong) => SqliteType.Integer,
            _ => SqliteType.Text
        };

        internal static object ConvertType(Type propertyType, SqliteDataReader reader, int ordinal) => propertyType switch
        {
            Type _ when propertyType == typeof(bool) => reader.GetBoolean(ordinal),
            Type _ when propertyType == typeof(byte) => reader.GetByte(ordinal),
            Type _ when propertyType == typeof(byte[]) => (byte[])reader.GetValue(ordinal),
            Type _ when propertyType == typeof(char) => reader.GetChar(ordinal),
            Type _ when propertyType == typeof(char[]) => (char[])reader.GetValue(ordinal),
            Type _ when propertyType == typeof(DateOnly) => DateOnly.Parse(reader.GetString(ordinal)),
            Type _ when propertyType == typeof(DateTime) => reader.GetDateTime(ordinal),
            Type _ when propertyType == typeof(DateTimeOffset) => reader.GetDateTimeOffset(ordinal),
            Type _ when propertyType == typeof(decimal) => reader.GetDecimal(ordinal),
            Type _ when propertyType == typeof(double) => reader.GetDouble(ordinal),
            Type _ when propertyType == typeof(Guid) => reader.GetGuid(ordinal),
            Type _ when propertyType == typeof(short) => reader.GetInt16(ordinal),
            Type _ when propertyType == typeof(int) => reader.GetInt32(ordinal),
            Type _ when propertyType == typeof(long) => reader.GetInt64(ordinal),
            Type _ when propertyType == typeof(sbyte) => (sbyte)reader.GetInt16(ordinal),
            Type _ when propertyType == typeof(float) => reader.GetFloat(ordinal),
            Type _ when propertyType == typeof(string) => reader.GetString(ordinal),
            Type _ when propertyType == typeof(TimeOnly) => TimeOnly.Parse(reader.GetString(ordinal)),
            Type _ when propertyType == typeof(TimeSpan) => reader.GetTimeSpan(ordinal),
            Type _ when propertyType == typeof(ushort) => (ushort)reader.GetInt16(ordinal),
            Type _ when propertyType == typeof(uint) => (uint)reader.GetInt32(ordinal),
            Type _ when propertyType == typeof(ulong) => (ulong)reader.GetInt64(ordinal),
            _ => reader.GetValue(ordinal)
        };
    }
}
