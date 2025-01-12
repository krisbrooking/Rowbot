using Rowbot.Pipelines.Builder;
using Rowbot.Pipelines.Runner.DependencyResolution;
using Rowbot.UnitTests.Connectors.DataTable;
using Rowbot.UnitTests.Setup;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging.Abstractions;

namespace Rowbot.UnitTests.Pipelines.Runner.DependencyResolution
{
    public class EntityDependencyResolverTests
    {
        [Fact]
        public void Resolve_Should_ReturnEmptyList_WhenThereAreNoPipelines()
        {
            var resolver = new EntityDependencyResolver();

            var result = resolver.Resolve(new List<Pipeline>());

            Assert.Empty(result);
        }

        [Fact]
        public void Resolve_Should_ReturnSingleGroup_WhenPipelineDependenciesAreIsolated()
        {
            var resolver = new EntityDependencyResolver();
            var pipelines = new PipelineCollection()
                .Add(typeof(DataTableReadConnector<SourceCustomerDep, SourceCustomerDep>), typeof(DataTableWriteConnector<TargetCustomerDep>))
                .Add(typeof(DataTableReadConnector<SourceEmployeeDep, SourceEmployeeDep>), typeof(DataTableWriteConnector<TargetEmployeeDep>));

            var result = resolver.Resolve(pipelines.ToList());

            Assert.Single(result);
        }

        [Fact]
        public void Resolve_Should_ReturnTwoGroups_ForSimpleDependency()
        {
            var resolver = new EntityDependencyResolver();
            var pipelines = new PipelineCollection()
                .Add(typeof(DataTableReadConnector<SourceCustomerDep, SourceCustomerDep>), typeof(DataTableWriteConnector<TargetCustomerDep>))
                .Add(typeof(DataTableReadConnector<TargetCustomerDep, TargetCustomerDep>), typeof(DataTableWriteConnector<TargetInvoiceDep>));

            var result = resolver.Resolve(pipelines.ToList());

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void Resolve_Should_OrderByDependency_ForSimpleDependency()
        {
            var resolver = new EntityDependencyResolver();
            var pipelines = new PipelineCollection()
                .Add(typeof(DataTableReadConnector<SourceCustomerDep, SourceCustomerDep>), typeof(DataTableWriteConnector<TargetCustomerDep>))
                .Add(typeof(DataTableReadConnector<TargetCustomerDep, TargetCustomerDep>), typeof(DataTableWriteConnector<TargetInvoiceDep>));

            var result = resolver.Resolve(pipelines.ToList());

            Assert.Equal(typeof(TargetCustomerDep), result.First().First().Metadata.TargetEntityType);
        }

        [Fact]
        public void Resolve_Should_OrderByDependency_ForSimpleDependencyWherePipelinesInRandomOrder()
        {
            var resolver = new EntityDependencyResolver();
            var pipelines = new PipelineCollection()
                .Add(typeof(DataTableReadConnector<TargetCustomerDep, TargetCustomerDep>), typeof(DataTableWriteConnector<TargetInvoiceDep>))
                .Add(typeof(DataTableReadConnector<SourceCustomerDep, SourceCustomerDep>), typeof(DataTableWriteConnector<TargetCustomerDep>));

            var result = resolver.Resolve(pipelines.ToList());

            Assert.Equal(typeof(TargetCustomerDep), result.First().First().Metadata.TargetEntityType);
        }

        [Fact]
        public void Resolve_Should_IncludeAdditionalConnectorsInDependencyGraph()
        {
            var resolver = new EntityDependencyResolver();
            var pipelines = new PipelineCollection()
                .Add(typeof(DataTableReadConnector<SourceInvoiceDep, SourceInvoiceDep>), typeof(DataTableWriteConnector<TargetInvoiceDep>), typeof(DataTableWriteConnector<TargetCustomerDep>))
                .Add(typeof(DataTableReadConnector<SourceCustomerDep, SourceCustomerDep>), typeof(DataTableWriteConnector<TargetCustomerDep>));

            var result = resolver.Resolve(pipelines.ToList());

            Assert.Equal(typeof(TargetInvoiceDep), result.Last().First().Metadata.TargetEntityType);
        }

        [Fact]
        public void Resolve_Should_ShareGroup_ForIsolatedDependenciesOfSingleNode()
        {
            var resolver = new EntityDependencyResolver();
            var pipelines = new PipelineCollection()
                .Add(typeof(DataTableReadConnector<SourceCustomerDep, SourceCustomerDep>), typeof(DataTableWriteConnector<TargetCustomerDep>))
                .Add(typeof(DataTableReadConnector<SourceTerritoryDep, SourceTerritoryDep>), typeof(DataTableWriteConnector<TargetTerritoryDep>))
                .Add(typeof(DataTableReadConnector<SourceInvoiceDep, SourceInvoiceDep>), typeof(DataTableWriteConnector<TargetInvoiceDep>), typeof(DataTableWriteConnector<TargetCustomerDep>), typeof(DataTableWriteConnector<TargetTerritoryDep>));

            var result = resolver.Resolve(pipelines.ToList());

            Assert.Equal(2, result.Count());
            Assert.Equal(typeof(TargetInvoiceDep), result.Last().First().Metadata.TargetEntityType);
        }

        [Fact]
        public void Resolve_Should_ShareGroups_ForMultipleIsolatedDependencyGraphs()
        {
            var resolver = new EntityDependencyResolver();
            var pipelines = new PipelineCollection()
                .Add(typeof(DataTableReadConnector<SourceCustomerDep, SourceCustomerDep>), typeof(DataTableWriteConnector<TargetCustomerDep>))
                .Add(typeof(DataTableReadConnector<SourceTerritoryDep, SourceTerritoryDep>), typeof(DataTableWriteConnector<TargetTerritoryDep>))
                .Add(typeof(DataTableReadConnector<SourceInvoiceDep, SourceInvoiceDep>), typeof(DataTableWriteConnector<TargetInvoiceDep>), typeof(DataTableWriteConnector<TargetCustomerDep>), typeof(DataTableWriteConnector<TargetTerritoryDep>))

                .Add(typeof(DataTableReadConnector<SourceEmployeeDep, SourceEmployeeDep>), typeof(DataTableWriteConnector<TargetEmployeeDep>))
                .Add(typeof(DataTableReadConnector<SourceProjectDep, SourceProjectDep>), typeof(DataTableWriteConnector<TargetProjectDep>))
                .Add(typeof(DataTableReadConnector<SourceTimeSheetDep, SourceTimeSheetDep>), typeof(DataTableWriteConnector<TargetTimeSheetDep>), typeof(DataTableWriteConnector<TargetEmployeeDep>), typeof(DataTableWriteConnector<TargetProjectDep>));

            var result = resolver.Resolve(pipelines.ToList());

            Assert.Equal(2, result.Count());
            Assert.Equal(new List<Type>() { typeof(TargetInvoiceDep), typeof(TargetTimeSheetDep) }, result.Last().Select(x => x.Metadata.TargetEntityType!).ToList());
        }

        [Fact]
        public void Resolve_Should_GroupSameTargetType_ForMultiplePipelinesWithSameTargetType()
        {
            var resolver = new EntityDependencyResolver();
            var pipelines = new PipelineCollection()
                .Add(typeof(DataTableReadConnector<SourceTerritoryDep, SourceTerritoryDep>), typeof(DataTableWriteConnector<TargetTerritoryDep>))
                .Add(typeof(DataTableReadConnector<SourceCustomerDep, SourceCustomerDep>), typeof(DataTableWriteConnector<TargetCustomerDep>), typeof(DataTableWriteConnector<TargetTerritoryDep>))
                .Add(typeof(DataTableReadConnector<TargetCustomerDep, TargetCustomerDep>), typeof(DataTableWriteConnector<TargetCustomerDep>), typeof(DataTableWriteConnector<TargetTerritoryDep>))
                .Add(typeof(DataTableReadConnector<SourceInvoiceDep, SourceInvoiceDep>), typeof(DataTableWriteConnector<TargetInvoiceDep>), typeof(DataTableWriteConnector<TargetCustomerDep>), typeof(DataTableWriteConnector<TargetTerritoryDep>));

            var result = resolver.Resolve(pipelines.ToList());

            Assert.Equal(3, result.Count());
            Assert.Equal(typeof(TargetTerritoryDep), result.First().First().Metadata.TargetEntityType);
            Assert.Equal(typeof(TargetInvoiceDep), result.Last().First().Metadata.TargetEntityType);
        }

        [Fact]
        public void Resolve_ShouldThrowFrameworkException_ForCircularDependency()
        {
            var resolver = new EntityDependencyResolver();
            var pipelines = new PipelineCollection()
                .Add(typeof(DataTableReadConnector<TargetPerson, TargetPerson>), typeof(DataTableWriteConnector<SourcePerson>))
                .Add(typeof(DataTableReadConnector<SourcePerson, SourcePerson>), typeof(DataTableWriteConnector<TargetPerson>));

            Assert.Throws<InvalidOperationException>(() => resolver.Resolve(pipelines.ToList()));
        }

        [Fact]
        public void Resolve_ShouldThrowFrameworkException_ForCircularDependencyBetweenThreeNodes()
        {
            var resolver = new EntityDependencyResolver();
            var pipelines = new PipelineCollection()
                .Add(typeof(DataTableReadConnector<TargetPerson, TargetPerson>), typeof(DataTableWriteConnector<AnnotatedEntity>))
                .Add(typeof(DataTableReadConnector<AnnotatedEntity, AnnotatedEntity>), typeof(DataTableWriteConnector<SourcePerson>))
                .Add(typeof(DataTableReadConnector<SourcePerson, SourcePerson>), typeof(DataTableWriteConnector<TargetPerson>));

            Assert.Throws<InvalidOperationException>(() => resolver.Resolve(pipelines.ToList()));
        }

        public class PipelineCollection : Collection<Pipeline>
        {
            public PipelineCollection() : base(new List<Pipeline>()) { }

            public PipelineCollection(IList<Pipeline> list) : base(list) { }

            public PipelineCollection Add(Type sourceConnector, Type targetConnector, params Type[] additionalConnectors)
            {
                var context = new PipelineBuilderContext(new NullLoggerFactory(), new ServiceFactory(type => type));
                
                context.DependencyResolution.SetTargetEntity(targetConnector);
                context.DependencyResolution.AddSourceEntity(sourceConnector);

                foreach (var additionalConnector in additionalConnectors)
                {
                    context.DependencyResolution.AddSourceEntity(additionalConnector);
                }
                var pipeline = new Pipeline(context);

                Add(pipeline);

                return this;
            }
        }

        public class SourceCustomerDep { }
        public class TargetCustomerDep { }
        public class SourceTerritoryDep { }
        public class TargetTerritoryDep { }
        public class SourceInvoiceDep { }
        public class TargetInvoiceDep { }
        public class SourceEmployeeDep { }
        public class TargetEmployeeDep { }
        public class SourceProjectDep { }
        public class TargetProjectDep { }
        public class SourceTimeSheetDep { }
        public class TargetTimeSheetDep { }
    }
}
