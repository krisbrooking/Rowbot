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
        public SqlRetryLogicBaseProvider SqlRetryLogicBaseProvider { get; set; } =
            SqlConfigurableRetryFactory.CreateExponentialRetryProvider(new SqlRetryLogicOption
            {
                NumberOfTries = 5,
                DeltaTime = TimeSpan.FromSeconds(1),
                MaxTimeInterval = TimeSpan.FromSeconds(20),
            });
    }
}
