using Rowbot.Framework.Blocks.Extractors.Pagination;
using Rowbot.UnitTests.Setup;
using System.Linq;
using Xunit;

namespace Rowbot.UnitTests.Framework.Blocks.Extractors.Pagination
{
    public class OffsetDataPagerTests
    {
        [Fact]
        public void Next_Should_ReturnInitialValue_OnFirstInvocation()
        {
            var dataPager = new OffsetDataPager<SourcePerson>(1, OffsetOrder.Ascending);

            var parameters = dataPager.Next();

            Assert.Equal(1, (int)parameters.First(x => x.ParameterName == "Offset").ParameterValue!);
        }

        [Fact]
        public void Next_Should_ReturnIncrementingOffset_WhenAscendingOffsetOrder()
        {
            var dataPager = new OffsetDataPager<SourcePerson>(0, OffsetOrder.Ascending);
            dataPager.Next();
            dataPager.AddResults(Enumerable.Range(1, 5).Select(x => new SourcePerson { Id = x }).ToArray());

            var parameters = dataPager.Next();

            Assert.Equal(5, (int)parameters.First(x => x.ParameterName == "Offset").ParameterValue!);
        }

        [Fact]
        public void Next_Should_ReturnDecrementingOffset_WhenDescendingOffsetOrder()
        {
            var dataPager = new OffsetDataPager<SourcePerson>(5, OffsetOrder.Descending);
            dataPager.Next();
            dataPager.AddResults(Enumerable.Range(1, 5).Select(x => new SourcePerson { Id = x }).ToArray());

            var parameters = dataPager.Next();

            Assert.Equal(0, (int)parameters.First(x => x.ParameterName == "Offset").ParameterValue!);
        }

        [Fact]
        public void Next_Should_ReturnCustomParameter_ForCustomOffsetParameter()
        {
            var dataPager = new OffsetDataPager<SourcePerson>(0, OffsetOrder.Ascending, "CustomParameter");

            var parameters = dataPager.Next();

            Assert.Equal(0, (int)parameters.First(x => x.ParameterName == "CustomParameter").ParameterValue!);
        }

        [Fact]
        public void Next_Should_SetIsEndOfQueryAfterFirstPage_ForEmptyResults()
        {
            var dataPager = new OffsetDataPager<SourcePerson>(0, OffsetOrder.Ascending);

            dataPager.Next();
            dataPager.Next();

            Assert.True(dataPager.IsEndOfQuery);
        }
    }
}
