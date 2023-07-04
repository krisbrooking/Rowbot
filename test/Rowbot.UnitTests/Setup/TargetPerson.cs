using System;
using System.ComponentModel.DataAnnotations;

namespace Rowbot.UnitTests.Setup
{
    public class TargetPerson : Row
    {
        public TargetPerson() { }
        public TargetPerson(int id, string? firstName, string? lastName)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
        }
        [Key]
        public int Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
    }
}
