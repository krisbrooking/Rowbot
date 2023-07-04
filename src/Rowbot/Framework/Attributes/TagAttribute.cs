namespace Rowbot
{
    public class TagAttribute : Attribute
    {
        public TagAttribute(params string[] tags)
        {
            Tags = tags ?? Array.Empty<string>();
        }

        public string[] Tags { get; private set; }
    }
}
