namespace Rowbot
{
    public class ClusterAttribute : Attribute
    {
        public ClusterAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
    }
}
