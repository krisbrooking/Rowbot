using System.Linq.Expressions;

namespace Rowbot.Transformers.Mappers.Actions;

/// <summary>
/// Wraps a mapper action expression in an <see cref="ITargetMapperAction{TTarget}"/> interface.
/// For use when extending <see cref="MapperConfiguration{TSource, TTarget}"/> from an extension method.
/// </summary>
public sealed class TargetTransformAction<TSource, TTarget> : ITargetMapperAction<TTarget>
{
    /// <inheritdoc cref="TargetTransformAction{TSource, TTarget}"/>
    public TargetTransformAction(Expression<Action<TTarget>> mapperExpression)
    {
        Apply = mapperExpression.Compile();
    }

    /// <inheritdoc cref="TargetTransformAction{TSource, TTarget}"/>
    public TargetTransformAction(Action<TTarget> action)
    {
        Apply = action;
    }

    public TargetMapperActionType ActionType => TargetMapperActionType.Transform;
    public Action<TTarget> Apply { get; }
}
