using Rowbot.Framework.Blocks.Transformers.Mappers.Transforms;
using Rowbot.UnitTests.Setup;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using Xunit;

namespace Rowbot.UnitTests.Framework.Blocks.Transformers.Mappers.Transforms
{
    public class HashCodeTransformTests
    {
        [Fact]
        public void Include_Should_AddPropertyToCollection()
        {
            IHashCodeTransform<SourcePerson> transform = new HashCodeTransform<SourcePerson>();
            var hashCodeGenerator = transform.Include(x => x.Id);
            Assert.Single(hashCodeGenerator.Selected);
        }

        [Fact]
        public void Include_Should_ThrowArgumentException_ForInvalidSelector()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                IHashCodeTransform<SourcePerson> transform = new HashCodeTransform<SourcePerson>();
                var hashCodeGenerator = transform.Include(x => x.Id == 1);
            });
        }

        [Fact]
        public void Include_Should_AddMultiplePropertiesToCollection_ForChainedValidSelectors()
        {
            IHashCodeTransform<SourcePerson> transform = new HashCodeTransform<SourcePerson>();
            var hashCodeGenerator = transform.Include(x => x.Id).Include(x => x.First_Name);
            Assert.Equal(2, hashCodeGenerator.Selected.Count());
        }

        [Fact]
        public void Include_Should_NotDuplicatePropertyInCollection()
        {
            IHashCodeTransform<SourcePerson> transform = new HashCodeTransform<SourcePerson>();
            var hashCodeGenerator = transform.Include(x => x.Id).Include(x => x.Id);
            Assert.Single(hashCodeGenerator.Selected);
        }

        [Fact]
        public void All_Should_AddAllPropertiesToCollection()
        {
            IHashCodeTransform<SourcePerson> transform = new HashCodeTransform<SourcePerson>();
            var hashCodeGenerator = transform.All();
            Assert.Equal(3, hashCodeGenerator.Selected.Count());
        }

        [Fact]
        public void All_Should_IgnoreRowProperties_WhenEntityInheritsFromRow()
        {
            IHashCodeTransform<TargetPerson> transform = new HashCodeTransform<TargetPerson>();
            var hashCodeGenerator = transform.All();
            Assert.Equal(4, hashCodeGenerator.Selected.Count());
        }

        [Fact]
        public void Build_Should_ReturnDelegate_ForSingleProperty()
        {
            IHashCodeTransform<SourcePerson> transform = new HashCodeTransform<SourcePerson>();

            var hashCode = transform.Include(x => x.Id).Build()(SourcePerson.GetValidEntity());
            var result = Convert.ToBase64String(hashCode);

            Assert.Equal("2kN1yKJgI+jtps3pmLygD+Dy1Vo=", result);
        }

        [Fact]
        public void Build_Should_ReturnDelegate_ForAllProperties()
        {
            IHashCodeTransform<SourcePerson> transform = new HashCodeTransform<SourcePerson>();

            var hashCode = transform.All().Build()(SourcePerson.GetValidEntity());
            var result = Convert.ToBase64String(hashCode);

            Assert.Equal("Uubl482Z1ceGadgqYgFFAspmcOg=", result);
        }

        [Fact]
        public void Build_Should_ReturnDelegate_ForAllWithSinglePropertyExcluded()
        {
            IHashCodeTransform<SourcePerson> transform = new HashCodeTransform<SourcePerson>();

            var hashCode = transform.All().Exclude(x => x.First_Name).Build()(SourcePerson.GetValidEntity());
            var result = Convert.ToBase64String(hashCode);

            Assert.Equal("lCja+09HsrDBDGG2MrucEYaBKfk=", result);
        }

        [Fact]
        public void HashCodeTransform_Should_ReturnIdenticalHashCodes_WhenDifferentEntitiesHaveIdenticalPropertiesAndValues()
        {
            IHashCodeTransform<SourcePerson> sourceTransform = new HashCodeTransform<SourcePerson>();
            IHashCodeTransform<TargetPerson> targetTransform = new HashCodeTransform<TargetPerson>();

            var sourceHashCode = sourceTransform.Include(x => x.Id).Build()(SourcePerson.GetValidEntity());
            var targetHashCode = targetTransform.Include(x => x.Id).Build()(new TargetPerson { Id = 1 });

            Assert.Equal(Convert.ToBase64String(sourceHashCode), Convert.ToBase64String(targetHashCode));
        }

        [Fact]
        public void HashCodeTransform_Should_ReturnDifferentHashCodes_WhenDifferentEntitiesHaveIdenticalPropertiesAndValuesButDifferentSeeds()
        {
            IHashCodeTransform<SourcePerson> sourceTransform = new HashCodeTransform<SourcePerson>();
            IHashCodeTransform<TargetPerson> targetTransform = new HashCodeTransform<TargetPerson>();

            var sourceHashCode = sourceTransform.WithSeed(1).Include(x => x.Id).Build()(SourcePerson.GetValidEntity());
            var targetHashCode = targetTransform.WithSeed(2).Include(x => x.Id).Build()(new TargetPerson { Id = 1 });

            Assert.NotEqual(Convert.ToBase64String(sourceHashCode), Convert.ToBase64String(targetHashCode));
        }

        [Fact]
        public void HashCodeTransform_Should_ReturnIdenticalHashCodes_WhenEntitiesHaveSameDateTimeValueFromDifferentCultures()
        {
            IHashCodeTransform<TargetPerson> culture1Transform = new HashCodeTransform<TargetPerson>();
            IHashCodeTransform<TargetPerson> culture2Transform = new HashCodeTransform<TargetPerson>();

            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            var culture1HashCode = culture1Transform.Include(x => x.DateOfBirth).Build()(new TargetPerson { DateOfBirth = new DateTime(1970, 01, 01) });
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-NZ");
            var culture2HashCode = culture2Transform.Include(x => x.DateOfBirth).Build()(new TargetPerson { DateOfBirth = new DateTime(1970, 01, 01) });

            Assert.Equal(Convert.ToBase64String(culture1HashCode), Convert.ToBase64String(culture2HashCode));
        }
    }
}
