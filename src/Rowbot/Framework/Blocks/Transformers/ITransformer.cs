using Rowbot.Framework.Pipelines.Options;

namespace Rowbot
{
    public interface ITransformer<TSource, TTarget>
    {
        Task<TTarget[]> TransformAsync(TSource[] source);
    }

    public interface ITransformer<TSource, TTarget, TOptions> : ITransformer<TSource, TTarget>
        where TOptions : TransformerOptions
    {
        TOptions Options { get; set; }
    }
}
