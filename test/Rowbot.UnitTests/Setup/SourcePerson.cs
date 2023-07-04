using System.Collections.Generic;

namespace Rowbot.UnitTests.Setup
{
    public class SourcePerson
    {
        public SourcePerson() { }
        public SourcePerson(int id, string firstName, string lastName)
        {
            Id = id;
            First_Name = firstName;
            Last_Name = lastName;
        }
        public int Id { get; set; }
        public string? First_Name { get; set; }
        public string? Last_Name { get; set; }

        /// <returns>new SourcePerson(1, "Alice", "Anderson")</returns>
        public static SourcePerson GetValidEntity() => new SourcePerson(1, "Alice", "Anderson");

        /// <returns>{ new SourcePerson(1, "Alice", "Anderson"), new SourcePerson(2, "Bob", "Brown") }</returns>
        public static List<SourcePerson> GetValidEntities() => new List<SourcePerson>() { new SourcePerson(1, "Alice", "Anderson"), new SourcePerson(2, "Bob", "Brown") };
    }
}
