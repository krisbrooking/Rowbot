namespace Rowbot.Extractors.Framework;

public class ExtractContext<TInput>
{
    private readonly int _batchSize;

    public ExtractContext(int batchSize)
    {
        _batchSize = batchSize;
    }

    public ExtractContext(int batchSize, TInput input)
    {
        _batchSize = batchSize;
        Input = input;
    }

    public TInput? Input { get; }

    public ExtractParameter[] GetParameters()
    {
        var parameters = Input is null
            ? []
            : Input.GetType().GetProperties()
                .Select(x => new ExtractParameter(x.Name, x.PropertyType, x.GetValue(Input)));

        return [new ExtractParameter("BatchSize", typeof(int), _batchSize), ..parameters];
    }
}