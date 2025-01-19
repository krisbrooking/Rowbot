namespace Rowbot.Extractors.Framework;

public class ExtractContext<TInput>
{
    private readonly int _batchSize;
    private readonly ExtractParameter[] _userDefinedParameters;

    public ExtractContext(int batchSize, ExtractParameter[] parameters)
    {
        _batchSize = batchSize;
        _userDefinedParameters = parameters;
    }

    public ExtractContext(int batchSize, TInput input, ExtractParameter[] parameters)
    {
        _batchSize = batchSize;
        Input = input;
        _userDefinedParameters = parameters;
    }

    public TInput? Input { get; }

    public ExtractParameter[] GetParameters()
    {
        var inputParameters = Input is null
            ? []
            : Input.GetType().GetProperties()
                .Select(x => new ExtractParameter(x.Name, x.PropertyType, x.GetValue(Input)));

        return [new ExtractParameter("BatchSize", typeof(int), _batchSize), ..inputParameters, .._userDefinedParameters];
    }
}