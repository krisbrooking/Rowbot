namespace Rowbot.Transformers.Default
{
    public sealed class DefaultTransformer<TSource, TTarget> : ITransformer<TSource, TTarget, DefaultTransformerOptions<TSource, TTarget>>
    {
        public DefaultTransformerOptions<TSource, TTarget> Options { get; set; } = new();

        public async Task<TTarget[]> TransformAsync(TSource[] source)
        {
            return await Options.Transform(source, Options.Mapper);
        }
    }
}
