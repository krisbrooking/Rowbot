using Rowbot.Common;
using Rowbot.Framework.Blocks.Transformers.Mappers.Actions;
using Rowbot.Framework.Blocks.Transformers.Mappers.Configuration;
using System.Linq.Expressions;

namespace Rowbot.Framework.Blocks.Transformers.Mappers.Transforms
{
    public static class ConstantValueTransformExtensions
    {
        public static MapperConfiguration<TSource, TTarget> ToConstantValue<TSource, TTarget, TProperty>(
            this TransformMapperConfiguration<TSource, TTarget> configuration,
            Expression<Func<TTarget, TProperty>> targetPropertySelector,
            TProperty constantValue)
        {
            MemberExpression targetMemberExpression = Ensure.ArgumentIsMemberExpression(targetPropertySelector);

            var mapper = new TargetTransformAction<TTarget, TTarget>(GetConstantValueExpression<TTarget, TProperty>(targetMemberExpression, constantValue));

            return configuration.AddTarget(mapper);
        }

        private static Expression<Action<TTarget>> GetConstantValueExpression<TTarget, TProperty>(MemberExpression targetMemberExpression, TProperty constantValue)
        {
            var targetParameter = Expression.Parameter(typeof(TTarget), "target");

            var propertyAccessor = Expression.MakeMemberAccess(targetParameter, targetMemberExpression.Member);

            var body = Expression.Assign(propertyAccessor, Expression.Constant(constantValue));

            return Expression.Lambda<Action<TTarget>>(body, targetParameter);
        }
    }
}
