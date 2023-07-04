namespace Rowbot
{
    public interface ITransformer<TSource, TTarget>
    {
        Task<TTarget[]> TransformAsync(TSource[] source);
    }

    public interface ITransformer<TSource, TTarget, TOptions> : ITransformer<TSource, TTarget>
    {
        TOptions Options { get; set; }
    }
}
