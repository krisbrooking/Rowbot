namespace Rowbot;

public interface ITestResult
{
    string SourceString { get; }
    string TargetString { get; }
}

public class TestResult<TProperty>(TProperty source, TProperty target) : ITestResult
{
    public TestResult(TProperty source) : this(source, source)
    {
    }

    public TProperty Source { get; } = source;
    public TProperty Target { get; } = target;
    public string SourceString => Source?.ToString() ?? string.Empty;
    public string TargetString => Target?.ToString() ?? string.Empty;
}