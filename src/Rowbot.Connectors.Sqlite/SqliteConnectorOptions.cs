using Microsoft.Data.Sqlite;

namespace Rowbot.Connectors.Sqlite
{
    public class SqliteConnectorOptions
    {
        internal string ConnectionString { get; set; } = string.Empty;
        public int MaxParametersPerCommand { get; set; } = 999;
        public int CommandTimeout { get; set; } = 30;
    }

    public sealed class SqliteReadConnectorOptions<TEntity> : SqliteConnectorOptions
    {
        public SqliteReadConnectorOptions() { }
        public SqliteReadConnectorOptions(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public bool HasCommands => Commands is not null;
        public IEnumerable<SqliteCommand>? Commands { get; set; }
        public bool HasQuery => !string.IsNullOrEmpty(Query);
        internal string? Query { get; set; }
    }

    public sealed class SqliteWriteConnectorOptions<TEntity> : SqliteConnectorOptions
    {
        public SqliteWriteConnectorOptions() { }
        public SqliteWriteConnectorOptions(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public bool TruncateTable { get; set; }
        internal List<SqlitePipelineCommand> PipelineCommands { get; private set; } = new();
        public void AddPrePipelineCommand(string command, int priority = 60)
            => PipelineCommands.Add(new SqlitePipelineCommand(command, priority));
        public void AddPostPipelineCommand(string command, int priority = 101)
            => PipelineCommands.Add(new SqlitePipelineCommand(command, priority));
    }

    internal sealed class SqlitePipelineCommand
    {
        public SqlitePipelineCommand(string command, int priority)
        {
            Command = command;
            Priority = priority;
        }

        public string Command { get; set; }
        public int Priority { get; set; }
    }
}
