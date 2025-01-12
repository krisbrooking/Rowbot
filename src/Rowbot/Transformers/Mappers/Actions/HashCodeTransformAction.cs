using Rowbot.Common;
using Rowbot.Transformers.Mappers.Transforms;
using System.Linq.Expressions;

namespace Rowbot.Transformers.Mappers.Actions;

internal sealed class HashCodeTransformAction<TEntity> : ITargetMapperAction<TEntity>
{
    public HashCodeTransformAction(Action<IHashCodeTransform<TEntity>> hashCodeTransform, Expression<Func<TEntity, byte[]>> targetPropertySelector)
    {
        MemberExpression targetPropertyExpression = Ensure.ArgumentIsMemberExpression(targetPropertySelector);

        Apply = GetMapperExpression(hashCodeTransform, targetPropertyExpression).Compile();
    }

    /// <summary>
    /// Converts hash code delegate and target property selector Func{TEntity,TProperty}(target => target.KeyHash) 
    /// into mapper action, Action{TEntity}({ target.KeyHash = hashCode(source) })
    /// </summary>
    private Expression<Action<TEntity>> GetMapperExpression(Action<IHashCodeTransform<TEntity>> hashCodeTransform, MemberExpression targetPropertyExpression)
    {
        Ensure.ArgumentIsNotNull(hashCodeTransform);

        var transform = new HashCodeTransform<TEntity>();
        hashCodeTransform(transform);

        var hashCodeDelegate = ((IHashCodeSelection<TEntity>)transform).Build();

        var targetType = typeof(TEntity);
        var targetTypeParameter = Expression.Parameter(targetType, "target");
        var targetProperty = targetPropertyExpression.Member;

        var invokeSourceExpression = Expression.Invoke(Expression.Constant(hashCodeDelegate), targetTypeParameter);

        var body = Expression.Assign(Expression.MakeMemberAccess(targetTypeParameter, targetProperty), invokeSourceExpression);
        var lambda = Expression.Lambda<Action<TEntity>>(body, targetTypeParameter);

        return lambda;
    }

    public TargetMapperActionType ActionType => TargetMapperActionType.Transform;
    public Action<TEntity> Apply { get; }
}