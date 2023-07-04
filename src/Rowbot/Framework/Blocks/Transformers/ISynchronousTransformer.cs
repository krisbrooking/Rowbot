namespace Rowbot
{
    internal interface ISynchronousTransformer<TSource, TTarget>
    {
        TTarget[] Transform(TSource[] source);
    }

    internal interface ISynchronousTransformer<TSource, TTarget, TOptions> : ISynchronousTransformer<TSource, TTarget>
    {
        TOptions Options { get; set; }
    }
}
