namespace Rowbot.Entities.DataAnnotations
{
    public class PrecisionAttribute : Attribute
    {
        public PrecisionAttribute(int precision, int scale)
        {
            Precision = precision;
            Scale = scale;
        }

        public int Precision { get; set; }
        public int Scale { get; set; }
    }
}
