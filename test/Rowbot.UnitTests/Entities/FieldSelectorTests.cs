using Rowbot.Entities;
using Rowbot.UnitTests.Setup;
using System;
using System.Linq;
using Xunit;

namespace Rowbot.UnitTests.Entities
{
    public class FieldSelectorTests
    {
        [Fact]
        public void Include_Should_AddFieldDescriptorToCollection()
        {
            var fieldSelector = FieldSelector<SourcePerson>.Create().Include(x => x.Id);
            Assert.Single(fieldSelector.Selected);
        }

        [Fact]
        public void Include_Should_ThrowArgumentException_ForInvalidSelector()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var fieldSelector = FieldSelector<SourcePerson>.Create().Include(x => x.Id == 1);
            });
        }

        [Fact]
        public void Include_Should_AddMultipleFieldDescriptorsToCollection()
        {
            var fieldSelector = FieldSelector<SourcePerson>.Create().Include(x => x.Id).Include(x => x.First_Name);
            Assert.Equal(2, fieldSelector.Selected.Count());
        }

        [Fact]
        public void Include_Should_NotDuplicateFieldDescriptorInCollection()
        {
            var fieldSelector = FieldSelector<SourcePerson>.Create().Include(x => x.Id).Include(x => x.Id);
            Assert.Single(fieldSelector.Selected);
        }

        [Fact]
        public void All_Should_AddAllFieldDescriptorsToCollection()
        {
            var fieldSelector = FieldSelector<SourcePerson>.Create().All();
            Assert.Equal(3, fieldSelector.Selected.Count());
        }
    }
}
