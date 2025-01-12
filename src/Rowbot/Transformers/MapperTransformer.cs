namespace Rowbot;

public sealed class MapperTransformer<TInput, TOutput> : ITransformer<TInput, TOutput>
{
    public Mapper<TInput, TOutput> Mapper { get; set; } = new();

    public TOutput[] Transform(TInput[] source)
    {
        return Mapper.Apply(source);
    }
}