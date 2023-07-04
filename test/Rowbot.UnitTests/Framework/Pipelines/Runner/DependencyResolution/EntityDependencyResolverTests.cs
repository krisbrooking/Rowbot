using Rowbot.Framework;
using Rowbot.Framework.Pipelines.Builder;
using Rowbot.Framework.Pipelines.Runner.DependencyResolution;
using Rowbot.UnitTests.Connectors.DataTable;
using Rowbot.UnitTests.Setup;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;

namespace Rowbot.UnitTests.Framework.Pipelines.Runner.DependencyResolution
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
            var pipelineDefinitions = new PipelineDefinitionCollection()
                .Add(typeof(DataTableReadConnector<SourceCustomerDep>), typeof(DataTableWriteConnector<TargetCustomerDep>))
                .Add(typeof(DataTableReadConnector<SourceEmployeeDep>), typeof(DataTableWriteConnector<TargetEmployeeDep>));

            var result = resolver.Resolve(pipelineDefinitions.ToList());

            Assert.Single(result);
        }

        [Fact]
        public void Resolve_Should_ReturnTwoGroups_ForSimpleDependency()
        {
            var resolver = new EntityDependencyResolver();
            var pipelineDefinitions = new PipelineDefinitionCollection()
                .Add(typeof(DataTableReadConnector<SourceCustomerDep>), typeof(DataTableWriteConnector<TargetCustomerDep>))
                .Add(typeof(DataTableReadConnector<TargetCustomerDep>), typeof(DataTableWriteConnector<TargetInvoiceDep>));

            var result = resolver.Resolve(pipelineDefinitions.ToList());

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void Resolve_Should_OrderByDependency_ForSimpleDependency()
        {
            var resolver = new EntityDependencyResolver();
            var pipelineDefinitions = new PipelineDefinitionCollection()
                .Add(typeof(DataTableReadConnector<SourceCustomerDep>), typeof(DataTableWriteConnector<TargetCustomerDep>))
                .Add(typeof(DataTableReadConnector<TargetCustomerDep>), typeof(DataTableWriteConnector<TargetInvoiceDep>));

            var result = resolver.Resolve(pipelineDefinitions.ToList());

            Assert.Equal(typeof(TargetCustomerDep), result.First().First().Metadata.TargetEntityType);
        }

        [Fact]
        public void Resolve_Should_OrderByDependency_ForSimpleDependencyWherePipelinesInRandomOrder()
        {
            var resolver = new EntityDependencyResolver();
            var pipelineDefinitions = new PipelineDefinitionCollection()
                .Add(typeof(DataTableReadConnector<TargetCustomerDep>), typeof(DataTableWriteConnector<TargetInvoiceDep>))
                .Add(typeof(DataTableReadConnector<SourceCustomerDep>), typeof(DataTableWriteConnector<TargetCustomerDep>));

            var result = resolver.Resolve(pipelineDefinitions.ToList());

            Assert.Equal(typeof(TargetCustomerDep), result.First().First().Metadata.TargetEntityType);
        }

        [Fact]
        public void Resolve_Should_IncludeAdditionalConnectorsInDependencyGraph()
        {
            var resolver = new EntityDependencyResolver();
            var pipelineDefinitions = new PipelineDefinitionCollection()
                .Add(typeof(DataTableReadConnector<SourceInvoiceDep>), typeof(DataTableWriteConnector<TargetInvoiceDep>), typeof(DataTableWriteConnector<TargetCustomerDep>))
                .Add(typeof(DataTableReadConnector<SourceCustomerDep>), typeof(DataTableWriteConnector<TargetCustomerDep>));

            var result = resolver.Resolve(pipelineDefinitions.ToList());

            Assert.Equal(typeof(TargetInvoiceDep), result.Last().First().Metadata.TargetEntityType);
        }

        [Fact]
        public void Resolve_Should_ShareGroup_ForIsolatedDependenciesOfSingleNode()
        {
            var resolver = new EntityDependencyResolver();
            var pipelineDefinitions = new PipelineDefinitionCollection()
                .Add(typeof(DataTableReadConnector<SourceCustomerDep>), typeof(DataTableWriteConnector<TargetCustomerDep>))
                .Add(typeof(DataTableReadConnector<SourceTerritoryDep>), typeof(DataTableWriteConnector<TargetTerritoryDep>))
                .Add(typeof(DataTableReadConnector<SourceInvoiceDep>), typeof(DataTableWriteConnector<TargetInvoiceDep>), typeof(DataTableWriteConnector<TargetCustomerDep>), typeof(DataTableWriteConnector<TargetTerritoryDep>));

            var result = resolver.Resolve(pipelineDefinitions.ToList());

            Assert.Equal(2, result.Count());
            Assert.Equal(typeof(TargetInvoiceDep), result.Last().First().Metadata.TargetEntityType);
        }

        [Fact]
        public void Resolve_Should_ShareGroups_ForMultipleIsolatedDependencyGraphs()
        {
            var resolver = new EntityDependencyResolver();
            var pipelineDefinitions = new PipelineDefinitionCollection()
                .Add(typeof(DataTableReadConnector<SourceCustomerDep>), typeof(DataTableWriteConnector<TargetCustomerDep>))
                .Add(typeof(DataTableReadConnector<SourceTerritoryDep>), typeof(DataTableWriteConnector<TargetTerritoryDep>))
                .Add(typeof(DataTableReadConnector<SourceInvoiceDep>), typeof(DataTableWriteConnector<TargetInvoiceDep>), typeof(DataTableWriteConnector<TargetCustomerDep>), typeof(DataTableWriteConnector<TargetTerritoryDep>))

                .Add(typeof(DataTableReadConnector<SourceEmployeeDep>), typeof(DataTableWriteConnector<TargetEmployeeDep>))
                .Add(typeof(DataTableReadConnector<SourceProjectDep>), typeof(DataTableWriteConnector<TargetProjectDep>))
                .Add(typeof(DataTableReadConnector<SourceTimeSheetDep>), typeof(DataTableWriteConnector<TargetTimeSheetDep>), typeof(DataTableWriteConnector<TargetEmployeeDep>), typeof(DataTableWriteConnector<TargetProjectDep>));

            var result = resolver.Resolve(pipelineDefinitions.ToList());

            Assert.Equal(2, result.Count());
            Assert.Equal(new List<Type>() { typeof(TargetInvoiceDep), typeof(TargetTimeSheetDep) }, result.Last().Select(x => x.Metadata.TargetEntityType!).ToList());
        }

        [Fact]
        public void Resolve_Should_GroupSameTargetType_ForMultiplePipelinesWithSameTargetType()
        {
            var resolver = new EntityDependencyResolver();
            var pipelineDefinitions = new PipelineDefinitionCollection()
                .Add(typeof(DataTableReadConnector<SourceTerritoryDep>), typeof(DataTableWriteConnector<TargetTerritoryDep>))
                .Add(typeof(DataTableReadConnector<SourceCustomerDep>), typeof(DataTableWriteConnector<TargetCustomerDep>), typeof(DataTableWriteConnector<TargetTerritoryDep>))
                .Add(typeof(DataTableReadConnector<TargetCustomerDep>), typeof(DataTableWriteConnector<TargetCustomerDep>), typeof(DataTableWriteConnector<TargetTerritoryDep>))
                .Add(typeof(DataTableReadConnector<SourceInvoiceDep>), typeof(DataTableWriteConnector<TargetInvoiceDep>), typeof(DataTableWriteConnector<TargetCustomerDep>), typeof(DataTableWriteConnector<TargetTerritoryDep>));

            var result = resolver.Resolve(pipelineDefinitions.ToList());

            Assert.Equal(3, result.Count());
            Assert.Equal(typeof(TargetTerritoryDep), result.First().First().Metadata.TargetEntityType);
            Assert.Equal(typeof(TargetInvoiceDep), result.Last().First().Metadata.TargetEntityType);
        }

        [Fact]
        public void Resolve_ShouldThrowFrameworkException_ForCircularDependency()
        {
            var resolver = new EntityDependencyResolver();
            var pipelineDefinitions = new PipelineDefinitionCollection()
                .Add(typeof(DataTableReadConnector<TargetPerson>), typeof(DataTableWriteConnector<SourcePerson>))
                .Add(typeof(DataTableReadConnector<SourcePerson>), typeof(DataTableWriteConnector<TargetPerson>));

            Assert.Throws<FrameworkException>(() => resolver.Resolve(pipelineDefinitions.ToList()));
        }

        [Fact]
        public void Resolve_ShouldThrowFrameworkException_ForCircularDependencyBetweenThreeNodes()
        {
            var resolver = new EntityDependencyResolver();
            var pipelineDefinitions = new PipelineDefinitionCollection()
                .Add(typeof(DataTableReadConnector<TargetPerson>), typeof(DataTableWriteConnector<AnnotatedEntity>))
                .Add(typeof(DataTableReadConnector<AnnotatedEntity>), typeof(DataTableWriteConnector<SourcePerson>))
                .Add(typeof(DataTableReadConnector<SourcePerson>), typeof(DataTableWriteConnector<TargetPerson>));

            Assert.Throws<FrameworkException>(() => resolver.Resolve(pipelineDefinitions.ToList()));
        }

        public class PipelineDefinitionCollection : Collection<Pipeline>
        {
            public PipelineDefinitionCollection() : base(new List<Pipeline>()) { }

            public PipelineDefinitionCollection(IList<Pipeline> list) : base(list) { }

            public PipelineDefinitionCollection Add(Type sourceConnector, Type targetConnector, params Type[] additionalConnectors)
            {
                var metadata = new DependencyResolutionMetadata();
                metadata.SetTargetEntity(targetConnector);
                metadata.AddSourceEntity(sourceConnector);

                foreach (var additionalConnector in additionalConnectors)
                {
                    metadata.AddSourceEntity(additionalConnector);
                }
                var pipelineDefinition = new PipelineDefinition();
                pipelineDefinition.DependencyResolution = metadata;
                var pipeline = new Pipeline(pipelineDefinition);

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
