using Microsoft.Data.SqlClient;

namespace Rowbot.Connectors.SqlServer
{
    public class SqlServerConnectorOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
        public int MaxParametersPerCommand { get; set; } = 999;
        public int CommandTimeout { get; set; } = 30;
    }

    public sealed class SqlServerReadConnectorOptions<TEntity> : SqlServerConnectorOptions
    {
        public bool HasCommands => Commands is not null;
        public IEnumerable<SqlCommand>? Commands { get; set; }
        public bool HasQuery => !string.IsNullOrEmpty(Query);
        public string? Query { get; set; }
    }

    public sealed class SqlServerWriteConnectorOptions<TEntity> : SqlServerConnectorOptions
    {
        public bool TruncateTable { get; set; }
        internal List<SqlServerPipelineCommand> PipelineCommands { get; private set; } = new();
        public void AddPrePipelineCommand(string command, int priority = 60)
            => PipelineCommands.Add(new SqlServerPipelineCommand(command, priority));
        public void AddPostPipelineCommand(string command, int priority = 101)
            => PipelineCommands.Add(new SqlServerPipelineCommand(command, priority));
    }

    internal sealed class SqlServerPipelineCommand
    {
        public SqlServerPipelineCommand(string command, int priority)
        {
            Command = command;
            Priority = priority;
        }

        public string Command { get; set; }
        public int Priority { get; set; }
    }
}
