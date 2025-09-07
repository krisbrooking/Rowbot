using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rowbot.Common;

namespace Rowbot;

public sealed class TestTransformer<TInput, TOutput, TProperty> : ITransformer<TInput, TOutput>
{
    private ILogger<TestTransformer<TInput, TOutput, TProperty>> _logger = new NullLogger<TestTransformer<TInput, TOutput, TProperty>>();
    private Func<TInput, bool> _predicate = (input) => true;
    private Func<TInput, TProperty> _identifierSelector = (input) => default!;
    private LogLevel _logLevel = LogLevel.Warning;
    private string _logMessageFormat = string.Empty;
    private object?[] _logMessageArguments = [];
    private int _maxLogCount = 10;
    private int _logCount = 0;

    public void Init(
        Expression<Func<TInput, bool>> predicateExpression,
        Expression<Func<TInput, TProperty>> identifierExpression,
        ILoggerFactory loggerFactory,
        LogLevel logLevel,
        int maxLogs)
    {
        _predicate = predicateExpression.Compile();
        _identifierSelector = identifierExpression.Compile();

        var identifierMemberExpression = Ensure.ArgumentIsMemberExpression(identifierExpression);
        var identifierProperty = Ensure.MemberExpressionTargetsProperty(identifierMemberExpression);

        _logger = loggerFactory.CreateLogger<TestTransformer<TInput, TOutput, TProperty>>();
        _logLevel = logLevel;
        _logMessageFormat = "Test {Predicate} failed for {Entity} = {Identifier}";
        _logMessageArguments = [predicateExpression.Body.ToString(), identifierProperty.Name];
        _maxLogCount = maxLogs;
    }

    public TOutput[] Transform(TInput[] source)
    {
        foreach (var item in source)
        {
            if (!_predicate(item) && _logCount++ < _maxLogCount)
            {
                _logger.Log(_logLevel, _logMessageFormat, [.. _logMessageArguments, _identifierSelector(item)]);
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