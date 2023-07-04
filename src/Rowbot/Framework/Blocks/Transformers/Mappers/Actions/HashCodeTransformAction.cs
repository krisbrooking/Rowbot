using Rowbot.Common;
using Rowbot.Framework.Blocks.Transformers.Mappers.Transforms;
using System.Linq.Expressions;

namespace Rowbot.Framework.Blocks.Transformers.Mappers.Actions
{
    internal sealed class HashCodeTransformAction<TTarget> : ITargetMapperAction<TTarget>
    {
        public HashCodeTransformAction(Action<IHashCodeTransform<TTarget>> hashCodeTransform, Expression<Func<TTarget, byte[]>> targetPropertySelector)
        {
            MemberExpression targetPropertyExpression = Ensure.ArgumentIsMemberExpression(targetPropertySelector);

            Apply = GetMapperExpression(hashCodeTransform, targetPropertyExpression).Compile();
        }

        /// <summary>
        /// Converts hash code delegate and target property selector Func{TTarget,TProperty}(target => target.KeyHash) 
        /// into mapper action Action{TTarget}({ target.KeyHash = hashCode(source) })
        /// </summary>
        private Expression<Action<TTarget>> GetMapperExpression(Action<IHashCodeTransform<TTarget>> hashCodeTransform, MemberExpression targetPropertyExpression)
        {
            Ensure.ArgumentIsNotNull(hashCodeTransform);

            var transform = new HashCodeTransform<TTarget>();
            hashCodeTransform(transform);

            var hashCodeDelegate = ((IHashCodeSelection<TTarget>)transform).Build();

            var targetType = typeof(TTarget);
            var targetTypeParameter = Expression.Parameter(targetType, "target");
            var targetProperty = targetPropertyExpression.Member;

            var invokeSourceExpression = Expression.Invoke(Expression.Constant(hashCodeDelegate), targetTypeParameter);

            var body = Expression.Assign(Expression.MakeMemberAccess(targetTypeParameter, targetProperty), invokeSourceExpression);
            var lambda = Expression.Lambda<Action<TTarget>>(body, targetTypeParameter);

            return lambda;
        }

        public TargetMapperActionType ActionType => TargetMapperActionType.Transform;
        public Action<TTarget> Apply { get; }
    }
}
