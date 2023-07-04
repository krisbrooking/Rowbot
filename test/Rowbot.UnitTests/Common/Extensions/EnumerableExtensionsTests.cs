using Rowbot.Common.Extensions;
using Rowbot.UnitTests.Setup;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Rowbot.UnitTests.Common.Extensions
{
    public class EnumerableExtensionsTests
    {
        [Fact]
        public void ChunkBySum_Should_ChunkAllElements_WhenAllValuesAreSmallerThanChunkMaxSize()
        {
            var source = Enumerable.Range(1, 100).Select(x => new SourcePerson { Id = x });
            var chunks = source.ChunkBySum(100, x => x.Id).ToList();

            Assert.Equal(65, chunks.Count);
        }

        [Fact]
        public void ChunkBySum_Should_IgnoreElements_ThatAreLargerThanChunkMaxSize()
        {
            var source = Enumerable.Range(1, 100).Select(x => new SourcePerson { Id = x });
            var chunks = source.ChunkBySum(50, x => x.Id).ToList();

            Assert.Equal(33, chunks.Count);
        }

        public class ChunkEntity
        {
            public List<string>? Strings { get; set; }
        }
    }
}
