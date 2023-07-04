namespace Rowbot.Framework.Pipelines.Runner.DependencyResolution
{
    internal sealed class EntityDependencyResolver
    {
        public IEnumerable<IEnumerable<Pipeline>> Resolve(List<Pipeline> pipelineGroups)
        {
            var graphNodes = pipelineGroups.Select(x => new EntityGraphNode(x)).ToList();

            var connectorGroups = ResolveDependencies(graphNodes);

            return connectorGroups.Select(x => x.Select(c => c.Pipeline));
        }

        internal List<List<EntityGraphNode>> ResolveDependencies(List<EntityGraphNode> nodes)
        {
            (Dictionary<Type, List<EntityGraphNode>> Nodes, Dictionary<Type, int> Indegree) graph = CreateGraph(nodes);
            Queue<Type> startNodes = GetStartNodes(graph.Indegree);
            List<List<EntityGraphNode>> result = GroupNodes(startNodes, graph, nodes);
            CheckForCircularDependency(result, nodes);

            return result;
        }

        internal (Dictionary<Type, List<EntityGraphNode>> Nodes, Dictionary<Type, int> Indegree) CreateGraph(List<EntityGraphNode> nodes)
        {
            var graph = new Dictionary<Type, List<EntityGraphNode>>();
            var indegree = new Dictionary<Type, int>();

            foreach (var node in nodes)
            {
                if (!graph.ContainsKey(node.TargetType))
                {
                    graph[node.TargetType] = new List<EntityGraphNode>();
                }

                foreach (var dependency in node.Dependencies)
                {
                    if (!graph.ContainsKey(dependency))
                    {
                        graph[dependency] = new List<EntityGraphNode>();
                    }

                    graph[dependency].Add(node);
                }
            }

            foreach (var node in graph.Keys)
            {
                indegree[node] = 0;
            }

            foreach (var node in graph.Keys)
            {
                foreach (var next in graph[node])
                {
                    indegree[next.TargetType]++;
                }
            }

            return (graph, indegree);
        }

        internal Queue<Type> GetStartNodes(Dictionary<Type, int> indegree)
        {
            var queue = new Queue<Type>();

            foreach (var item in indegree.Where(x => x.Value == 0))
            {
                queue.Enqueue(item.Key);
            }

            return queue;
        }
        internal List<List<EntityGraphNode>> GroupNodes(Queue<Type> queue, (Dictionary<Type, List<EntityGraphNode>> Nodes, Dictionary<Type, int> Indegree) graph, List<EntityGraphNode> nodes)
        {
            var groups = new List<List<EntityGraphNode>>();
            while (queue.Count > 0)
            {
                var group = new List<EntityGraphNode>();
                int size = queue.Count;

                for (int i = 0; i < size; i++)
                {
                    Type nodeType = queue.Dequeue();

                    foreach (EntityGraphNode currentNode in nodes.Where(x => x.TargetType == nodeType))
                    {
                        group.Add(currentNode);
                    }

                    foreach (var next in graph.Nodes[nodeType])
                    {
                        graph.Indegree[next.TargetType]--;
                        if (graph.Indegree[next.TargetType] == 0)
                        {
                            queue.Enqueue(next.TargetType);
                        }
                    }
                }

                if (group.Count > 0)
                {
                    groups.Add(group);
                }
            }

            return groups;
        }

        internal void CheckForCircularDependency(List<List<EntityGraphNode>> result, List<EntityGraphNode> nodes)
        {
            var resolvedNodes = result.SelectMany(group => group).Select(node => node.TargetType).ToList();

            if (resolvedNodes.Count != nodes.Count)
            {
                throw new FrameworkException("Circular Dependency detected");
            }
        }
    }
}
