using Rowbot.Framework.Blocks.Extractors.Parameters;

namespace Rowbot.Framework.Pipelines.Options
{
    public class ExtractorOptions
    {
        public ExtractorOptions() { }

        public ExtractorOptions(int batchSize)
        {
            BatchSize = batchSize;
        }

        public int BatchSize { get; set; } = 1000;

        public ExtractParameterGenerator ExtractParameterGenerator { get; set; } = new();
        public void AddParameter<T>(string parameterName, T? parameterValue, bool isNullable = false) 
            => ExtractParameterGenerator.Add(new ExtractParameter(parameterName, typeof(T), parameterValue, isNullable));
        public void AddParameter(ExtractParameter parameter) 
            => ExtractParameterGenerator.Add(parameter);
        public void AddParameters(ExtractParameterCollection parameters) 
            => ExtractParameterGenerator.Add(parameters);
        public void AddParameters(Func<IEnumerable<ExtractParameterCollection>> synchronousFactory) 
            => ExtractParameterGenerator.Add(synchronousFactory);
        public void AddParameters(Func<Task<IEnumerable<ExtractParameterCollection>>> asynchronousFactory) 
            => ExtractParameterGenerator.Add(asynchronousFactory);
    }
}
