using Rowbot.Connectors.Common.Find;
using Rowbot.Entities;
using Rowbot.UnitTests.Setup;

namespace Rowbot.UnitTests.Connectors.Common.Find
{
    public class FinderTests
    {
        [Fact]
        public void Finder_Should_CreateComparers()
        {
            var entityComparer = new EntityComparer<SourcePerson>(new EntityDescriptor<SourcePerson>());
            var findSelector = new FieldSelector<SourcePerson>().Include(x => x.Id).Include(x => x.First_Name);
            var resultSelector = new FieldSelector<SourcePerson>().Include(x => x.Last_Name);

            var finder = new Finder<SourcePerson>(findSelector, resultSelector, entityComparer);

            Assert.Equal(2, finder._comparers.Count);
        }

        [Fact]
        public void Finder_Should_CreateComparerThatReturnsTrueWhenValueFound()
        {
            var entityComparer = new EntityComparer<SourcePerson>(new EntityDescriptor<SourcePerson>());
            var sourcePerson = SourcePerson.GetValidEntity();
            var findSelector = new FieldSelector<SourcePerson>().Include(x => x.Id);
            var resultSelector = new FieldSelector<SourcePerson>().Include(x => x.First_Name);

            var finder = new Finder<SourcePerson>(findSelector, resultSelector, entityComparer);

            Assert.True(finder.Compare(sourcePerson, new SourcePerson { Id = 1 }));
        }

        [Fact]
        public void Finder_Should_CreateMappersForCompareAndResultSelectors_WhenMultipleResultFieldsSelected()
        {
            var entityComparer = new EntityComparer<SourcePerson>(new EntityDescriptor<SourcePerson>());
            var findSelector = new FieldSelector<SourcePerson>().Include(x => x.Id);
            var resultSelector = new FieldSelector<SourcePerson>().Include(x => x.First_Name).Include(x => x.Last_Name);

            var finder = new Finder<SourcePerson>(findSelector, resultSelector, entityComparer);

            Assert.Equal(3, finder._mappers.Count());
        }

        [Fact]
        public void Finder_Should_CreateMappersForUniqueFields_WhenDuplicateFieldsSelected()
        {
            var entityComparer = new EntityComparer<SourcePerson>(new EntityDescriptor<SourcePerson>());
            var findSelector = new FieldSelector<SourcePerson>().Include(x => x.Id).Include(x => x.First_Name);
            var resultSelector = new FieldSelector<SourcePerson>().Include(x => x.First_Name).Include(x => x.Last_Name);

            var finder = new Finder<SourcePerson>(findSelector, resultSelector, entityComparer);

            Assert.Equal(3, finder._mappers.Count());
        }

        [Fact]
        public void Return_Should_ExecuteCompareMapper()
        {
            var sourcePerson = SourcePerson.GetValidEntity();
            var entityComparer = new EntityComparer<SourcePerson>(new EntityDescriptor<SourcePerson>());
            var findSelector = new FieldSelector<SourcePerson>().Include(x => x.Id);
            var resultSelector = new FieldSelector<SourcePerson>().Include(x => x.First_Name);

            var finder = new Finder<SourcePerson>(findSelector, resultSelector, entityComparer);
            var targetPerson = finder.Return(sourcePerson);

            Assert.Equal(sourcePerson.Id, targetPerson.Id);
        }

        [Fact]
        public void Return_Should_ExecuteResultMapper()
        {
            var sourcePerson = SourcePerson.GetValidEntity();
            var entityComparer = new EntityComparer<SourcePerson>(new EntityDescriptor<SourcePerson>());
            var findSelector = new FieldSelector<SourcePerson>().Include(x => x.Id);
            var resultSelector = new FieldSelector<SourcePerson>().Include(x => x.First_Name);

            var finder = new Finder<SourcePerson>(findSelector, resultSelector, entityComparer);
            var targetPerson = finder.Return(sourcePerson);

            Assert.Equal(sourcePerson.First_Name, targetPerson.First_Name);
        }
    }
}
