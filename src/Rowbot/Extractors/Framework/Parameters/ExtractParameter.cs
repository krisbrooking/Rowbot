using Rowbot.Common;

namespace Rowbot;

public sealed record ExtractParameter
{
    public ExtractParameter(string parameterName, Type parameterType, object? parameterValue, bool isNullable = false)
    {
        Ensure.ArgumentIsNotNull(parameterName);
        Ensure.ArgumentIsNotNull(parameterType);

        ParameterName = parameterName;
        ParameterType = parameterType;
        ParameterValue = parameterValue;
        IsNullable = isNullable;
    }

    public ExtractParameter(string parameterCategory, string parameterName, Type parameterType, object? parameterValue, bool isNullable = false)
        : this(parameterName, parameterType, parameterValue, isNullable)
    {
        ParameterCategory = parameterCategory;
    }

    public string ParameterName { get; }
    public Type ParameterType { get; }
    public object? ParameterValue { get; }
    public bool IsNullable { get; }
    public string ParameterCategory { get; } = string.Empty;
}