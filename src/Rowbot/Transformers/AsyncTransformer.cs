namespace Rowbot;

public sealed class AsyncTransformer<TInput, TOutput> : IAsyncTransformer<TInput, TOutput>
{
    public Func<TInput[], Task<TOutput[]>> TransformDelegate { get; set; } = source => Task.FromResult(Array.Empty<TOutput>());

    public async Task<TOutput[]> TransformAsync(TInput[] source)
    {
        return await TransformDelegate(source);
    }
}