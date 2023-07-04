namespace Rowbot.Transformers.Default
{
    public sealed class DefaultSynchronousTransformer<TSource, TTarget> : ISynchronousTransformer<TSource, TTarget, DefaultSynchronousTransformerOptions<TSource, TTarget>>
    {
        public DefaultSynchronousTransformerOptions<TSource, TTarget> Options { get; set; } = new();

        public TTarget[] Transform(TSource[] source)
        {
            return Options.Transform(source, Options.Mapper);
        }
    }
}
