namespace Rowbot.Framework.Pipelines.Runner
{
    internal sealed class PipelineCluster
    {
        public PipelineCluster(string name)
        {
            Name = name;
        }

        public PipelineCluster(ClusterAttribute clusterAttribute)
        {
            Name = clusterAttribute.Name;
        }

        public string Name { get; private set; }

        public override bool Equals(object? obj)
        {
            if (obj == null || obj is not PipelineCluster pipelineCluster)
            {
                return false;
            }

            return Name == pipelineCluster.Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public static PipelineCluster Default => new PipelineCluster("Default");
    }
}
