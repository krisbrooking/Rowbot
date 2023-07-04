using System.Text.Json;

namespace Rowbot.Framework.Blocks
{
    public class BlockBuilderException : Exception
    {
        public BlockBuilderException(string message) : base(message) { }
        public BlockBuilderException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class BlockException : Exception
    {
        public BlockException(string message) : base(message)
        {
            RowsAffected = string.Empty;
        }

        public string RowsAffected { get; set; }
    }

    public class BlockException<T> : BlockException
    {
        public BlockException(IEnumerable<T> rows) : base(nameof(T))
        {
            RowsAffected = JsonSerializer.Serialize(rows);
        }
        public BlockException(string message, IEnumerable<T> rows) : base(message)
        {
            RowsAffected = JsonSerializer.Serialize(rows);
        }
    }
}
