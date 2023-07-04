namespace Rowbot.Transformers.Default
{
    public sealed class DefaultTransformerOptions<TSource, TTarget>
    {
        public Func<TSource[], Mapper<TSource, TTarget>, Task<TTarget[]>> Transform { get; set; } = (source, mapping) => Task.FromResult(Array.Empty<TTarget>());
        public Mapper<TSource, TTarget> Mapper { get; set; } = new();
    }
}
