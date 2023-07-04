using Rowbot.Framework;
using System.Reflection;

namespace Rowbot.DependencyInjection
{
    public class RowbotInstaller
    {
        private static readonly Lazy<RowbotInstaller> _instance = new Lazy<RowbotInstaller>(() => new RowbotInstaller());

        private RowbotInstaller()
        {
            Assemblies = new Assembly[1] { Assembly.GetEntryAssembly() ?? throw new FrameworkException($"Failed to get entry assembly. Pass assemblies to scan as an argument when calling services.AddRowbot()") };
        }

        public static RowbotInstaller Instance => _instance.Value;

        public Assembly[] Assemblies { get; set; }

        private IEnumerable<Type> GetPipelineTypes()
        {
            var typesInheritingPipeline = Assemblies
                .SelectMany(x => x.ExportedTypes)
                .Where(x => typeof(IPipelineContainer).IsAssignableFrom(x));

            foreach (var type in typesInheritingPipeline.Where(x => x != typeof(IPipelineContainer)))
            {
                yield return type;
            }
        }
    }
}
