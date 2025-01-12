using Microsoft.Data.Sqlite;

namespace Rowbot.Connectors.Sqlite
{
    public class SqliteConnectorOptions(string connectionString)
    {
        public string ConnectionString => connectionString;
        public int MaxParametersPerCommand { get; set; } = 999;
        public int CommandTimeout { get; set; } = 30;
    }

    public sealed class SqliteReadConnectorOptions<TEntity>(string connectionString)
        : SqliteConnectorOptions(connectionString)
    {
        public SqliteReadConnectorOptions() : this(string.Empty) { }

        public bool HasCommands => Commands is not null;
        public IEnumerable<SqliteCommand>? Commands { get; set; }
        public bool HasQuery => !string.IsNullOrEmpty(Query);
        internal string? Query { get; set; }
    }
}
