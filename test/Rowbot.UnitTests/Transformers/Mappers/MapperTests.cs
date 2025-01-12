using Rowbot.UnitTests.Setup;

namespace Rowbot.UnitTests.Transformers.Mappers
{
    public class MapperTests
    {
        [Fact]
        public void Apply_Should_MapPropertyThenExecuteTransform()
        {
            var mapper = new PersonMapper(
                new MapperConfiguration<SourcePerson, TargetPerson>()
                    .Map.Property(source => source.First_Name, target => target.LastName)
                    .Transform.AddSource((source, target) => { target.LastName = "Brown"; })
                    .Transform.ToHashCode(x => x.Include(h => h.Id), x => x.KeyHash));

            var source = SourcePerson.GetValidEntity();
            var target = mapper.Apply(source);

            Assert.Equal("Brown", target.LastName);
        }

        internal class PersonMapper : Mapper<SourcePerson, TargetPerson>
        {
            public PersonMapper(MapperConfiguration<SourcePerson, TargetPerson> configuration) : base(configuration) { }
        }
    }
}
