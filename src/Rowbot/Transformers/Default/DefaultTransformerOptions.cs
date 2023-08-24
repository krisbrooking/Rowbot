using Rowbot.Framework.Pipelines.Options;

namespace Rowbot.Transformers.Default
{
    public sealed class DefaultTransformerOptions<TSource, TTarget> : TransformerOptions
    {
        public Func<TSource[], Mapper<TSource, TTarget>, Task<TTarget[]>> Transform { get; set; } = (source, mapping) => Task.FromResult(Array.Empty<TTarget>());
        public Mapper<TSource, TTarget> Mapper { get; set; } = new();
    }
}
