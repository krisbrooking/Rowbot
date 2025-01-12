namespace Rowbot;

public class TagAttribute(params string[] tags) : Attribute
{
    public string[] Tags { get; private set; } = tags ?? [];
}