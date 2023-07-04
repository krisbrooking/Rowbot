using Microsoft.Extensions.Logging;
using System.Data;
using System.Text.RegularExpressions;

namespace Rowbot.Connectors.SqlServer
{
    public static class LoggerExtensions
    {
        public static void LogQuery<TSource>(this ILogger<SqlServerReadConnector<TSource>> logger, IDbCommand command)
            => logger.LogQueryOrCommand(command);

        public static void LogCommand<TTarget>(this ILogger<SqlServerWriteConnector<TTarget>> logger, IDbCommand command)
            => logger.LogQueryOrCommand(command);

        private static void LogQueryOrCommand(this ILogger logger, IDbCommand command)
        {
            var parameters = command.Parameters
                .Cast<IDbDataParameter>()
                .SelectMany(x => command.CommandText.GetAllIndexesOf(x.ParameterName).Select(c => (Index: c, x.ParameterName, Value: x.Value ?? "Unknown")))
                .ToList();

            var distinctParameters = new List<(int Index, string ParameterName, object Value)>();
            var commandText = command.CommandText;
            foreach (var parameter in parameters.OrderByDescending(x => x.ParameterName.Length))
            {
                if (!distinctParameters.Any(x => x.Index == parameter.Index))
                {
                    commandText = commandText.Replace(parameter.ParameterName, $"{{{parameter.ParameterName.Trim('@')}}}");
                    distinctParameters.Add(parameter);
                }
            }

            logger.LogDebug(commandText, distinctParameters.OrderBy(x => x.Index).Select(x => $"'{x.Value}'").ToArray());
        }

        private static List<int> GetAllIndexesOf(this string source, string substring)
            =>
            Regex.Matches(source, Regex.Escape(substring))
                .Select(x => x.Index)
                .ToList();
    }
}
