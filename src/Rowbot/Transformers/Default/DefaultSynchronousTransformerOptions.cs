namespace Rowbot.Transformers.Default
{
    public sealed class DefaultSynchronousTransformerOptions<TSource, TTarget>
    {
        public Func<TSource[], Mapper<TSource, TTarget>, TTarget[]> Transform { get; set; } = (source, mapping) => Array.Empty<TTarget>();
        public Mapper<TSource, TTarget> Mapper { get; set; } = new();
    }
}
