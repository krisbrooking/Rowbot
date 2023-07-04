using Rowbot.Framework.Blocks.Extractors.Pagination;
using Rowbot.UnitTests.Setup;
using System.Linq;
using Xunit;

namespace Rowbot.UnitTests.Framework.Blocks.Extractors.Pagination
{
    public class CursorDataPagerTests
    {
        [Fact]
        public void BuildNextCursorDelegate_Should_ReturnsCorrectExpression()
        {
            var dataPager = new CursorDataPager<SourcePerson, int>(x => x.Id, 0, CursorOrder.Ascending);

            var cursorProperty = typeof(SourcePerson).GetProperty("Id");
            var nextCursorExpression = dataPager.BuildNextCursorExpression(cursorProperty!);

            Assert.Equal("source => source.MaxBy(m => m)", nextCursorExpression.ToString());
        }

        [Fact]
        public void AddResults_Should_BeAddedToHashSet()
        {
            var dataPager = new CursorDataPager<SourcePerson, int>(x => x.Id, 0, CursorOrder.Ascending);

            dataPager.AddResults(new SourcePerson { Id = 1 }, new SourcePerson { Id = 2 });

            Assert.Equal(2, dataPager._data.Count);
        }

        [Fact]
        public void Next_Should_ReturnInitialValue_OnFirstInvocation()
        {
            var dataPager = new CursorDataPager<SourcePerson, int>(x => x.Id, 0, CursorOrder.Ascending);

            var parameters = dataPager.Next();

            Assert.Single(parameters);
        }

        [Fact]
        public void Next_Should_ReturnIncrementingCursor_WhenAscendingCursorOrder()
        {
            var dataPager = new CursorDataPager<SourcePerson, int>(x => x.Id, 0, CursorOrder.Ascending);
            dataPager.Next();
            dataPager.AddResults(new SourcePerson { Id = 2 }, new SourcePerson { Id = 3 });

            var parameters = dataPager.Next();

            Assert.Equal(3, (int)parameters.First(x => x.ParameterName == "Id").ParameterValue!);
        }

        [Fact]
        public void Next_Should_ReturnDecrementingCursor_WhenDescendingCursorOrder()
        {
            var dataPager = new CursorDataPager<SourcePerson, int>(x => x.Id, 3, CursorOrder.Descending);
            dataPager.Next();
            dataPager.AddResults(new SourcePerson { Id = 2 }, new SourcePerson { Id = 1 });

            var parameters = dataPager.Next();

            Assert.Equal(1, (int)parameters.First(x => x.ParameterName == "Id").ParameterValue!);
        }

        [Fact]
        public void Next_Should_ReturnCustomParameter_ForCustomCursorParameter()
        {
            var dataPager = new CursorDataPager<SourcePerson, int>(x => x.Id, 0, CursorOrder.Ascending, "CustomParameter");
            dataPager.AddResults(new SourcePerson { Id = 2 });

            var parameters = dataPager.Next();
            parameters = dataPager.Next();

            Assert.Equal(2, (int)parameters.First(x => x.ParameterName == "CustomParameter").ParameterValue!);
        }

        [Fact]
        public void Next_Should_SetIsEndOfQueryAfterFirstPage_ForEmptyResults()
        {
            var dataPager = new CursorDataPager<SourcePerson, int>(x => x.Id, 0, CursorOrder.Ascending);

            dataPager.Next();
            dataPager.Next();

            Assert.True(dataPager.IsEndOfQuery);
        }
    }
}
