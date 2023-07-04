using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rowbot.UnitTests.Setup
{
    [Table("AnnotatedEntities", Schema = "dbo")]
    public class AnnotatedEntity
    {
        [Key]
        public int Id { get; set; }

        [Column("ColumnName")]
        [Required]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Description { get; set; } = string.Empty;
    }

    public class AnnotatedEntityWithValidTimestamp : AnnotatedEntity
    {
        [Timestamp]
        public byte[] ValidTimestamp { get; set; } = new byte[0];
    }

    public class AnnotatedEntityWithInvalidTimestamp : AnnotatedEntity
    {
        [Timestamp]
        public string InvalidTimestamp { get; set; } = string.Empty;
    }

    public class AnnotatedEntityWithNonPublicProperties : AnnotatedEntity
    {
        protected string ProtectedProperty { get; set; } = string.Empty;
        private string PrivateProperty { get; set; } = string.Empty;
        internal string InternalProperty { get; set; } = string.Empty;
    }

    public class AnnotatedEntityWithPublicFields : AnnotatedEntity
    {
        public string PublicField = string.Empty;
    }

    public class AnnotatedEntityWithCompositeKeyColumnOrdering
    {
        [Key]
        [Column(Order = 2)]
        public int Id { get; set; }

        [Key]
        [Column(Order = 1)]
        public string Name { get; set; } = string.Empty;
    }

    public class AnnotatedEntityWithCompositeKeyNoColumnOrdering
    {
        [Key]
        public int Id { get; set; }

        [Key]
        public string Name { get; set; } = string.Empty;
    }
}
