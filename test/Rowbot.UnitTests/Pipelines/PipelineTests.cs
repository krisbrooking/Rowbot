using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rowbot.Entities;
using Rowbot.Pipelines.Blocks;
using Rowbot.Connectors.Common.Synchronisation;
using Rowbot.Extractors;
using Rowbot.Loaders;
using Rowbot.UnitTests.Connectors.DataTable;
using Rowbot.UnitTests.Setup;

namespace Rowbot.UnitTests.Pipelines
{
    public class PipelineTests
    {
        [Fact]
        public void LinkBlocks_Should_ThrowBlockBuilderException_WhenThereAreTooFewBlocks()
        {
            Assert.Throws<BlockBuilderException>(() =>
            {
                var blocks = CreateBlockQueue((GetValidExtractBlock()));

                Pipeline.LinkBlocks(blocks);
            });
        }

        [Fact]
        public void LinkBlocks_Should_BeLinkedInOrderAsSingleGroup()
        {
            var blocks = CreateBlockQueue(
                (GetValidExtractBlock()),
                (GetValidTransformerBlock()),
                (GetValidLoadBlock()));

            var result = Pipeline.LinkBlocks(blocks);

            Assert.Equal(3, result.Count);
            Assert.Equal("ExtractBlock`2,TransformBlock`2,LoadBlock`1", string.Join(',', result.Select(x => x.Target?.GetType().Name)));
        }

        [Fact]
        public void LinkBlocks_Should_ThrowBlockBuilderException_WhenDataflowBlockOrderIsInvalid()
        {
            Assert.Throws<BlockBuilderException>(() =>
            {
                var blocks = CreateBlockQueue(
                    (GetValidExtractBlock()),
                    (GetValidTransformerBlock()),
                    (GetValidTransformerBlock()));

                Pipeline.LinkBlocks(blocks);
            });
        }

        private Queue<IBlock> CreateBlockQueue(params IBlock[] items)
        {
            var blocks = new Queue<IBlock>();

            foreach (var item in items)
            {
                blocks.Enqueue(item);
            }

            return blocks;
        }

        private IBlock GetValidExtractBlock()
        {
            var connector = new DataTableReadConnector<SourcePerson, SourcePerson>(new Entity<SourcePerson>(), new SharedLockManager());
            var extractor = new DefaultExtractor<SourcePerson, SourcePerson>();
            extractor.Connector = connector;

            return new ExtractBlock<SourcePerson, SourcePerson>(extractor, new NullLoggerFactory(), 1000, new BlockOptions());
        }

        private IBlock GetValidTransformerBlock()
        {
            var transformer = new Transformer<SourcePerson, TargetPerson>();

            return new TransformBlock<SourcePerson, TargetPerson>(transformer, new NullLoggerFactory(), new BlockOptions());
        }

        private IBlock GetValidLoadBlock()
        {
            var loader = new DefaultLoader<TargetPerson>();

            return new LoadBlock<TargetPerson>(loader, new NullLoggerFactory(), new BlockOptions());
        }
    }
}
