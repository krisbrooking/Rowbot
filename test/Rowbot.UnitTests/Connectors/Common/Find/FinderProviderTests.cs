﻿using Rowbot.Entities;
using Rowbot.Connectors.Common.Find;
using Rowbot.UnitTests.Setup;

namespace Rowbot.UnitTests.Connectors.Common.Find
{
    public class FinderProviderTests
    {
        [Fact]
        public void CreateFinder_Should_CreatesFinder()
        {
            var entityComparer = new EntityComparer<SourcePerson>(new EntityDescriptor<SourcePerson>());
            var provider = new FinderProvider();
            var finder = provider.CreateFinder(compare => compare.Include(x => x.Id), result => result.Include(x => x.First_Name), entityComparer);

            Assert.IsAssignableFrom<Finder<SourcePerson>>(finder);
        }

        [Fact]
        public void CreateFinder_Should_GetFinderFromCache_OnSubsequentRequest()
        {
            var entityComparer = new EntityComparer<SourcePerson>(new EntityDescriptor<SourcePerson>());
            var provider = new FinderProvider();
            var finder1 = provider.CreateFinder(compare => compare.Include(x => x.Id), result => result.Include(x => x.First_Name), entityComparer);
            var finder2 = provider.CreateFinder(compare => compare.Include(x => x.Id), result => result.Include(x => x.First_Name), entityComparer);

            Assert.Same(finder1, finder2);
        }
    }
}
