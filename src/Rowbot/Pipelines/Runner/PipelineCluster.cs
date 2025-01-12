namespace Rowbot.Pipelines.Runner;

internal sealed class PipelineCluster(string name)
{
    private readonly string _name = name;
        
    public PipelineCluster(ClusterAttribute clusterAttribute) : this(clusterAttribute.Name)
    {
    }

    public string Name => _name;

    public override bool Equals(object? obj)
    {
        if (obj == null || obj is not PipelineCluster pipelineCluster)
        {
            return false;
        }

        return _name == pipelineCluster.Name;
    }

    public override int GetHashCode()
    {
        return _name.GetHashCode();
    }

    public static PipelineCluster Default => new PipelineCluster("Default");
}