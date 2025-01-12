namespace Rowbot;

public interface IAsyncTransformer<TInput, TOutput>
{
    Task<TOutput[]> TransformAsync(TInput[] source);
}