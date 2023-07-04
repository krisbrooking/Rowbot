using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rowbot
{
    public class Row
    {
        /// <summary>
        /// Deterministic hash code created by hashing one or more entity fields. 
        /// The KeyHash is designed to be unique even when columns like natural key are not (example: the same key from multiple sources).
        /// </summary>
        [MaxLength(20)]
        [Required]
        public byte[] KeyHash { get; set; } = new byte[20];
        /// <summary>
        /// Deterministic hash code created by hashing any entity fields that could change.
        /// This ChangeHash is used to quickly identify whether any field in a row has changed.
        /// </summary>
        [MaxLength(20)]
        [Required]
        public byte[] ChangeHash { get; set; } = new byte[20];
        /// <summary>
        /// Used to determine whether reporting of the entity should be ignored.
        /// </summary>
        public bool IsDeleted { get; set; }
        /// <summary>
        /// String representation of the KeyHash. This field is not persisted to the target system.
        /// </summary>
        [NotMapped]
        public string KeyHashBase64 => KeyHash.All(x => x == 0) ? string.Empty : Convert.ToBase64String(KeyHash);
        /// <summary>
        /// String representation of the ChangeHash. This field is not persisted to the target system.
        /// </summary>
        [NotMapped]
        public string ChangeHashBase64 => ChangeHash.All(x => x == 0) ? string.Empty : Convert.ToBase64String(ChangeHash);
    }

    public class Fact : Row
    {
        /// <summary>
        /// Date of entity creation in UTC.
        /// </summary>
        public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
    }

    public class Dimension : Row
    {
        /// <summary>
        /// Date of entity creation in UTC.
        /// </summary>
        public DateTimeOffset FromDate { get; set; } = DateTimeOffset.UtcNow;
        /// <summary>
        /// Date entity is valid until.
        /// </summary>
        public DateTimeOffset? ToDate { get; set; }
        /// <summary>
        /// Integer representation of FromDate (YYYYMMDD).
        /// </summary>
        public int FromDateKey => int.Parse(FromDate.ToString("yyyyMMdd"));
        /// <summary>
        /// Integer representation of ToDate (YYYYMMDD).
        /// </summary>
        public int? ToDateKey => ToDate.HasValue ? int.Parse(ToDate.Value.ToString("yyyyMMdd")) : null;
        /// <summary>
        /// Used to determine whether this instance of the entity is active (current). A dimension can span multiple
        /// instances (or rows) but only zero or one can be active. 
        /// <para>
        /// Note: When zero instances of an entity are active, the entity no longer exists on the source system but we still 
        /// want to report on it. This differs from IsDeleted which commonly means we don't want to report on the entity.
        /// </para>
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
