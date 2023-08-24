using Rowbot.Framework.Pipelines.Options;

namespace Rowbot
{
    internal interface ISynchronousTransformer<TSource, TTarget>
    {
        TTarget[] Transform(TSource[] source);
    }

    internal interface ISynchronousTransformer<TSource, TTarget, TOptions> : ISynchronousTransformer<TSource, TTarget>
        where TOptions : TransformerOptions
    {
        TOptions Options { get; set; }
    }
}
