using System;
using System.Data;

namespace Rowbot.UnitTests.Framework.Blocks.Connectors.Database
{
    public sealed class TestableDbCommand : IDbCommand
    {
        private readonly TestableDataParameterCollection _parameters;
        private string _commandText;

        public TestableDbCommand(string commandText)
        {
            _commandText = commandText;
            _parameters = new TestableDataParameterCollection();
        }

        public string CommandText
        {
            get { return _commandText; }
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
            set { _commandText = value; }
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
        }
        public int CommandTimeout { get; set; }
        public CommandType CommandType { get; set; }
        public IDbConnection? Connection { get; set; }

        public IDataParameterCollection Parameters => _parameters;

        public IDbTransaction? Transaction { get; set; }
        public UpdateRowSource UpdatedRowSource { get; set; }

        public void Cancel()
        {
            throw new NotImplementedException();
        }

        public IDbDataParameter CreateParameter()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public int ExecuteNonQuery()
        {
            throw new NotImplementedException();
        }

        public IDataReader ExecuteReader()
        {
            throw new NotImplementedException();
        }

        public IDataReader ExecuteReader(CommandBehavior behavior)
        {
            throw new NotImplementedException();
        }

        public object? ExecuteScalar()
        {
            throw new NotImplementedException();
        }

        public void Prepare()
        {
            throw new NotImplementedException();
        }
    }
}
