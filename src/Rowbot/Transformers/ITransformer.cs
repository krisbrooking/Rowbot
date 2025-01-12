namespace Rowbot;

public interface ITransformer<TInput, TOutput>
{
    TOutput[] Transform(TInput[] source);
}
