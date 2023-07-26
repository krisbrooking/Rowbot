using Rowbot.Framework.Pipelines.Runner;
using System.Text;

namespace Rowbot.Framework.Pipelines.Summary
{
    public sealed class ConsoleSummaryOutput : ISummaryOutput
    {
        public Task<bool> OutputAsync(IEnumerable<PipelineSummary> pipelineSummaries)
        {
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine();
            Console.WriteLine($"Total {GetSummary(pipelineSummaries, 5)}");

            if (pipelineSummaries.Count() == 0)
            {
                return Task.FromResult(false);
            }

            var groups = pipelineSummaries.GroupBy(x => x.Cluster);

            var totalRuntime = groups.Select(x => x.Select(y => y.Runtime).Aggregate((acc, curr) => acc.Add(curr))).Max();

            Console.WriteLine($"Total Runtime:{string.Empty.PadRight(17)}{totalRuntime.Minutes.ToString("00")}:{totalRuntime.Seconds.ToString("00")}:{totalRuntime.Milliseconds.ToString("00")}");

            if (groups.Count() == 1)
            {
                PrintExecutionSummary(pipelineSummaries);
            }
            else
            {
                foreach (var group in groups)
                {
                    Console.WriteLine($"'{TruncateClusterName(group.Key)}' {GetSummary(group.ToList(), 8 - group.Key.Length)}");
                    Console.WriteLine($"'{TruncateClusterName(group.Key)}' {GetRuntime(group.ToList(), 20 - group.Key.Length)}");
                }
                PrintExecutionSummary(pipelineSummaries);
            }

            PrintErrorSummary(pipelineSummaries);

            return Task.FromResult(true);

            string TruncateClusterName(string cluster) => cluster == nameof(PipelineCluster.Default) ? nameof(PipelineCluster.Default) : cluster.Length < 7 ? cluster : $"{cluster.Substring(0, 6)}…";
        }

        private void PrintExecutionSummary(IEnumerable<PipelineSummary> pipelineSummaries)
        {
            var windowWidth = SafeGetWindowWidth();

            Console.WriteLine(GetUnderscoreBorder(windowWidth));
            Console.WriteLine();

            if (windowWidth < 120)
            {
                Console.WriteLine("Terminal window must be at least 120 columns wide to display the table.");
                Console.WriteLine(GetUnderscoreBorder(windowWidth));
                Console.WriteLine();
                return;
            }

            Console.WriteLine(GetTableHeader(windowWidth));
            Console.WriteLine(GetHyphenBorder(windowWidth));

            var group = 1;
            foreach (var pipelineSummary in pipelineSummaries)
            {
                if (group != pipelineSummary.Group)
                {
                    Console.WriteLine(GetHyphenBorder(windowWidth));
                    group = pipelineSummary.Group;
                }

                Console.WriteLine(GetTableRow(pipelineSummary, windowWidth));
            }
        }

        private void PrintErrorSummary(IEnumerable<PipelineSummary> pipelineSummaries)
        {
            var pipelinesWithErrors = pipelineSummaries.Where(x => x.BlockSummaries.Any(x => !x.HasCompletedWithoutError));
            if (pipelinesWithErrors.Count() == 0)
            {
                return;
            }

            var windowWidth = SafeGetWindowWidth();
            
            Console.WriteLine();
            Console.WriteLine();
            var totalErrors = pipelinesWithErrors.SelectMany(x => x.BlockSummaries).SelectMany(x => x.Exceptions.Select(x => x.Value)).Count();
            Console.WriteLine($"Run finished with {totalErrors} error{(totalErrors == 1 ? string.Empty : "s")} in {pipelinesWithErrors.Count()} pipeline{(pipelinesWithErrors.Count() == 1 ? string.Empty : "s")}");
            Console.WriteLine(GetUnderscoreBorder(windowWidth));
            Console.WriteLine();
            foreach (var pipelineSummary in pipelinesWithErrors)
            {
                Console.WriteLine($"Cluster : {pipelineSummary.Cluster}, Group : {pipelineSummary.Group}, Container : {pipelineSummary.Container}, Pipeline : {pipelineSummary.Name}");
                foreach (var blockSummary in pipelineSummary.BlockSummaries.Where(x => !x.HasCompletedWithoutError))
                {
                    foreach (var exception in blockSummary.Exceptions)
                    {
                        Console.WriteLine($"Block : {blockSummary.Name}, Batch Number : {exception.Value.BatchNumber}");
                        Console.WriteLine(exception.Value.Exception.Message);
                        Console.WriteLine(exception.Value.Exception.StackTrace);
                        if (exception.Value.Exception.InnerException != null)
                        {
                            Console.WriteLine(exception.Value.Exception.InnerException.Message);
                            Console.WriteLine(exception.Value.Exception.InnerException.StackTrace);
                        }
                        Console.WriteLine(GetHyphenBorder(windowWidth));
                    }
                }
            }
        }

        private int SafeGetWindowWidth()
        {
            try
            {
                return Console.WindowWidth;
            }
            catch
            {
                return 120;
            }
        }

        private string GetSummary(IEnumerable<PipelineSummary> pipelineSummaries, int spacing)
            => $"Pipelines Completed:{string.Empty.PadRight(spacing)}{pipelineSummaries.Count(x => x.HasCompletedWithoutError)}/{pipelineSummaries.Count()}";

        private string GetRuntime(IEnumerable<PipelineSummary> pipelineSummaries, int spacing)
        {
            var totalRuntime = pipelineSummaries.Select(x => x.Runtime).Aggregate((acc, curr) => acc.Add(curr));

            return $"Runtime:{string.Empty.PadRight(spacing)}{totalRuntime.Minutes.ToString("00")}:{totalRuntime.Seconds.ToString("00")}:{totalRuntime.Milliseconds.ToString("00")}";
        }

        private string GetTableHeader(int windowWidth)
            => $"Cluster|Group|Container                   |Name{new String(' ', windowWidth - 81)}|Status |Runtime |Inserts |Updates";

        private string GetUnderscoreBorder(int windowWidth)
            => $"{new string(Enumerable.Range(0, windowWidth).Select(x => '_').ToArray())}";

        private string GetHyphenBorder(int windowWidth)
            => $"{new string(Enumerable.Range(0, windowWidth).Select(x => '-').ToArray())}";

        private string GetTableRow(PipelineSummary pipelineSummary, int windowWidth)
        {
            var rowStringBuilder = new StringBuilder();

            rowStringBuilder.Append(pipelineSummary.Cluster == nameof(PipelineCluster.Default) ? nameof(PipelineCluster.Default) : pipelineSummary.Cluster.Length < 7 ? pipelineSummary.Cluster.PadRight(7) : $"{pipelineSummary.Cluster.Substring(0, 5)}… ");
            rowStringBuilder.Append('|');
            var group = pipelineSummary.Group.ToString();
            rowStringBuilder.Append(group.Length < 5 ? group.PadRight(5) : group.Substring(0, 5));
            rowStringBuilder.Append('|');
            rowStringBuilder.Append(pipelineSummary.Container.Length < 28 ? pipelineSummary.Container.PadRight(28) : $"{pipelineSummary.Container.Substring(0, 26)}… ");
            rowStringBuilder.Append('|');
            var maxNameLength = windowWidth - 77;
            rowStringBuilder.Append(pipelineSummary.Name.Length < maxNameLength ? pipelineSummary.Name.PadRight(maxNameLength) : $"{pipelineSummary.Name.Substring(0, maxNameLength - 2)}… ");
            rowStringBuilder.Append('|');
            rowStringBuilder.Append(pipelineSummary.HasCompletedWithoutError ? "Success" : "Failed ");
            rowStringBuilder.Append('|');
            rowStringBuilder.Append(pipelineSummary.GetRuntime().Length < 8 ? pipelineSummary.GetRuntime().PadLeft(8) : pipelineSummary.GetRuntime().Substring(0, 8));
            rowStringBuilder.Append('|');
            var inserts = pipelineSummary.GetInserts().ToString();
            rowStringBuilder.Append(inserts.Length < 8 ? inserts.PadLeft(8) : inserts.Substring(0, 8));
            rowStringBuilder.Append('|');
            var updates = pipelineSummary.GetUpdates().ToString();
            rowStringBuilder.Append(updates.Length < 7 ? updates.PadLeft(7) : updates.Substring(0, 7));

            return rowStringBuilder.ToString();
        }
    }
}
