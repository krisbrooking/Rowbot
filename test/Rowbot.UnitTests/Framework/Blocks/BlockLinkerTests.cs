using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rowbot.Entities;
using Rowbot.Extractors.Default;
using Rowbot.Framework.Blocks;
using Rowbot.Framework.Blocks.Connectors.Synchronisation;
using Rowbot.Transformers.Default;
using Rowbot.UnitTests.Connectors.DataTable;
using Rowbot.UnitTests.Setup;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Rowbot.UnitTests.Framework.Blocks
{
    public class BlockLinkerTests
    {
        [Fact]
        public void LinkBlocks_Should_ThrowBlockBuilderException_WhenThereAreTooFewBlocks()
        {
            Assert.Throws<BlockBuilderException>(() =>
            {
                var blocks = CreateBlockQueue((GetValidExtractBlock(), 1));

                new BlockLinker().LinkBlocks(blocks, new BlockContext());
            });
        }

        [Fact]
        public void LinkBlocks_Should_BeLinkedInOrderAsSingleGroup()
        {
            var blocks = CreateBlockQueue(
                (GetValidExtractBlock(), 1),
                (GetValidTransformerBlock(), 2),
                (GetValidLoadBlock(), 3));

            var result = new BlockLinker().LinkBlocks(blocks, new BlockContext());

            Assert.Single(result);
            Assert.Equal("LoadBlock`1,TransformBlock`2,ExtractBlock`1", string.Join(',', result.Peek().Select(x => x.Target?.GetType().Name)));
        }

        [Fact]
        public void LinkBlocks_Should_ThrowBlockBuilderException_WhenDataflowBlockOrderIsInvalid()
        {
            Assert.Throws<BlockBuilderException>(() =>
            {
                var blocks = CreateBlockQueue(
                    (GetValidExtractBlock(), 1),
                    (GetValidTransformerBlock(), 2),
                    (GetValidTransformerBlock(), 3));

                new BlockLinker().LinkBlocks(blocks, new BlockContext());
            });
        }

        [Fact]
        public void LinkBlocks_Should_BeLinkedInOrderAsTwoGroups_WhenAdditionalTaskBlockIsIncludedAtStart()
        {
            var blocks = CreateBlockQueue(
                (new TaskBlock(() => Task.CompletedTask), 0),
                (GetValidExtractBlock(), 1),
                (GetValidTransformerBlock(), 2),
                (GetValidLoadBlock(), 3));

            var result = new BlockLinker().LinkBlocks(blocks, new BlockContext());

            Assert.Equal(2, result.Count);
            Assert.Equal("TaskBlock", string.Join(',', result.Pop().Select(x => x.Target?.GetType().Name)));
            Assert.Equal("LoadBlock`1,TransformBlock`2,ExtractBlock`1", string.Join(',', result.Pop().Select(x => x.Target?.GetType().Name)));
        }

        [Fact]
        public void LinkBlocks_ShouldBeLinkedInOrderAsTwoGroups_WhenAdditionalTaskBlockIsIncludedAtEnd()
        {
            var blocks = CreateBlockQueue(
                (GetValidExtractBlock(), 1),
                (GetValidTransformerBlock(), 2),
                (GetValidLoadBlock(), 3),
                (new TaskBlock(() => Task.CompletedTask), 10));

            var result = new BlockLinker().LinkBlocks(blocks, new BlockContext());

            Assert.Equal(2, result.Count);
            Assert.Equal("LoadBlock`1,TransformBlock`2,ExtractBlock`1", string.Join(',', result.Pop().Select(x => x.Target?.GetType().Name)));
            Assert.Equal("TaskBlock", string.Join(',', result.Pop().Select(x => x.Target?.GetType().Name)));
        }

        private PriorityQueue<IBlock, int> CreateBlockQueue(params (IBlock Block, int Priority)[] items)
        {
            var blocks = new PriorityQueue<IBlock, int>(Comparer<int>.Create((x, y) => y - x));

            foreach (var item in items)
            {
                blocks.Enqueue(item.Block, item.Priority);
            }

            return blocks;
        }

        private IBlock GetValidExtractBlock()
        {
            var connector = new DataTableReadConnector<SourcePerson>(new Entity<SourcePerson>(), new SharedLockManager());
            var extractor = new DefaultExtractor<SourcePerson>();
            var options = new DefaultExtractorOptions<SourcePerson>();
            extractor.Options = options;
            extractor.Connector = connector;

            return new ExtractBlock<SourcePerson>(extractor, new NullLoggerFactory());
        }

        private IBlock GetValidTransformerBlock()
        {
            var transformer = new DefaultTransformer<SourcePerson, TargetPerson>();

            return new TransformBlock<SourcePerson, TargetPerson>(transformer, new NullLoggerFactory(), 1);
        }

        private IBlock GetValidLoadBlock()
        {
            var logger = new LoggerFactory().CreateLogger<RowLoader<TargetPerson>>();
            var loader = new RowLoader<TargetPerson>(logger);

            return new LoadBlock<TargetPerson>(loader, new NullLoggerFactory(), 1);
        }
    }
}
