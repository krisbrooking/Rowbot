using Rowbot.Entities;
using System.Data;

namespace Rowbot.Framework.Blocks.Connectors.Database
{
    public interface IDbCommandProvider
    {
        IDbCommand CreateCommand(string commandText);
        IDbCommand AddOrUpdateParameter(IDbCommand command, ExtractParameter extractParameter);
        IDbCommand AddParameters(IDbCommand command, IEnumerable<ExtractParameter> extractParameters);
        string GetDatabaseType(FieldDescriptor field);
    }
}
