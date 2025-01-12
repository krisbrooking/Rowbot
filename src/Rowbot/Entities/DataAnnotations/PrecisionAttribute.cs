namespace Rowbot.Entities.DataAnnotations;

public class PrecisionAttribute(int precision, int scale) : Attribute
{
    public int Precision { get; set; } = precision;
    public int Scale { get; set; } = scale;
}