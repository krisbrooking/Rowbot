namespace Rowbot;

public sealed class EnsureTransformer<TInput, TOutput> : ITransformer<TInput, TOutput>
    where TInput : ITestResult
{
    public Func<TInput, bool> Predicate { get; set; } = (input) => true;
    public string PredicateBody { get; set; } = string.Empty;

    public TOutput[] Transform(TInput[] source)
    {
        foreach (var item in source)
        {
            if (!Predicate(item))
            {
                throw new EnsureException($"Failed on {PredicateBody}, Source = {item.SourceString}, Target = {item.TargetString}");
            }
        }

        return source.Select(Apply).ToArray();
    }

    private TOutput Apply(TInput input)
    {
        if (input is TOutput output)
        {
            return output;
        }

        return default!;
    }
}