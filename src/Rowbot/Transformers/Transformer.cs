namespace Rowbot;

public sealed class Transformer<TInput, TOutput> : ITransformer<TInput, TOutput>
{
    public Func<TInput[], TOutput[]> TransformDelegate { get; set; } = source => [];

    public TOutput[] Transform(TInput[] source)
    {
        return TransformDelegate(source);
    }
}