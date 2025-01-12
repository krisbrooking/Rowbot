using Rowbot.Entities;
using Rowbot.Connectors.Common.Database;
using System.Data;

namespace Rowbot.UnitTests.Connectors.Common.Database
{
    public sealed class TestableDbCommandProvider : IDbCommandProvider
    {
        public IDbCommand AddConnection(IDbCommand command, IDbConnection connection)
        {
            throw new NotImplementedException();
        }

        public IDbCommand AddOrUpdateParameter(IDbCommand command, ExtractParameter extractParameter)
        {
            if (command.Parameters is TestableDataParameterCollection parameterCollection)
            {
                if (parameterCollection.Contains(extractParameter.ParameterName))
                {
                    var index = parameterCollection.IndexOf(extractParameter.ParameterName);
                    parameterCollection[index].Value = extractParameter.ParameterValue;
                }
                else
                {
                    parameterCollection.Add(new TestableDataParameter($"@{extractParameter.ParameterName.Trim('@')}", extractParameter.ParameterValue!));
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

        public IDbCommand CreateCommand(string commandText) => new TestableDbCommand(commandText);

        public string GetDatabaseType(FieldDescriptor field) => $"{field.Property.PropertyType.Name}({field.MaxLength})";

        public string CreateDataSetCommandTextPostProcessor(string commandText) => commandText;

        public string TruncateDataSetCommandTextPostProcessor(string commandText) => commandText;
    }
}
