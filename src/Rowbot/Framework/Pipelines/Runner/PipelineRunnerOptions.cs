namespace Rowbot.Framework.Pipelines.Runner
{
    public sealed class PipelineRunnerOptions
    {
        public void FilterByTag(params string[] tags)
        {
            Tags = tags;
        }

        public void FilterByCluster(params string[] clusters)
        {
            Clusters = clusters;
        }

        internal string[] Tags { get; private set; } = new string[0];
        internal string[] Clusters { get; private set; } = new string[0];
    }
}
