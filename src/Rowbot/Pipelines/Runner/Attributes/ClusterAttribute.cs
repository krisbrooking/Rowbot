namespace Rowbot;

public class ClusterAttribute(string name) : Attribute
{
    public string Name { get; private set; } = name;
}