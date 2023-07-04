using System;
using System.Data;

namespace Rowbot.UnitTests.Framework.Blocks.Connectors.Database
{
    public sealed class TestableDataParameter : IDataParameter
    {
        private string _parameterName;
        private string _sourceColumn;

        public TestableDataParameter(string parameterName, object parameterValue)
        {
            _parameterName = parameterName;
            _sourceColumn = parameterName;
            Value = parameterValue;
        }

        public DbType DbType { get; set; }
        public ParameterDirection Direction { get; set; }
        public bool IsNullable => throw new NotImplementedException();
        public string ParameterName
        {
            get { return _parameterName; }
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
            set { _parameterName = value; }
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
        }
        public string SourceColumn
        {
            get { return _sourceColumn; }
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
            set { _sourceColumn = value; }
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
        }
        public DataRowVersion SourceVersion { get; set; }
        public object? Value { get; set; }
    }
}
