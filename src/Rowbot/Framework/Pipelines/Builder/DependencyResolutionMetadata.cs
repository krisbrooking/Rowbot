using Rowbot.Common.Extensions;

namespace Rowbot.Framework.Pipelines.Builder
{
    public sealed class DependencyResolutionMetadata
    {
        private readonly List<Type> _sourceEntityTypes;
        private Type? _targetEntityType;

        public DependencyResolutionMetadata()
        {
            _sourceEntityTypes = new();
        }

        public IEnumerable<Type> SourceEntityTypes => _sourceEntityTypes;
        public Type? TargetEntityType => _targetEntityType;


        public void SetTargetEntity(Type connectorOrLoaderType)
        {
            if (connectorOrLoaderType.IsGenericType &&
               (connectorOrLoaderType.ImplementsGenericInterface(typeof(IWriteConnector<,>)) || connectorOrLoaderType.ImplementsGenericInterface(typeof(ILoader<,>))))
            {
                _targetEntityType = connectorOrLoaderType.GetGenericArguments()[0];
            }
        }

        public void AddSourceEntity(Type connectorOrEntityType)
        {
            if (connectorOrEntityType.IsGenericType &&
               (connectorOrEntityType.ImplementsGenericInterface(typeof(IReadConnector<,>)) || connectorOrEntityType.ImplementsGenericInterface(typeof(IWriteConnector<,>))))
            {
                _sourceEntityTypes.Add(connectorOrEntityType.GetGenericArguments()[0]);
            }
            else if (!connectorOrEntityType.IsAbstract && !connectorOrEntityType.IsInterface && !connectorOrEntityType.IsGenericType)
            {
                _sourceEntityTypes.Add(connectorOrEntityType);
            }
        }
    }
}
