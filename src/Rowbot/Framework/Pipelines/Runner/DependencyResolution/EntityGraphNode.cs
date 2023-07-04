using Rowbot.Common;

namespace Rowbot.Framework.Pipelines.Runner.DependencyResolution
{
    internal sealed class EntityGraphNode
    {
        public EntityGraphNode(Pipeline pipeline)
        {
            var targetEntityType = Ensure.ArgumentIsNotNull(pipeline.Metadata.TargetEntityType, nameof(pipeline.Metadata.TargetEntityType))!;

            Pipeline = pipeline;
            TargetType = targetEntityType;
            Dependencies = pipeline.Metadata.SourceEntityTypes
                .Where(x => x != targetEntityType)
                .ToHashSet();
        }

        public Pipeline Pipeline { get; set; }
        public Type TargetType { get; set; }
        public HashSet<Type> Dependencies { get; }
    }
}
