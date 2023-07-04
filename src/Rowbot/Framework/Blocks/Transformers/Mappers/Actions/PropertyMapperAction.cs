using System.Linq.Expressions;

namespace Rowbot.Framework.Blocks.Transformers.Mappers.Actions
{
    /// <summary>
    /// Builds a mapper action from source and target property selectors
    /// </summary>
    internal sealed class PropertyMapperAction<TSource, TTarget, TProperty> : ISourceMapperAction<TSource, TTarget>
    {
        public PropertyMapperAction(MemberExpression sourceMemberExpression, MemberExpression targetMemberExpression)
        {
            Apply = GetMapperExpression(sourceMemberExpression, targetMemberExpression).Compile();
        }

        /// <summary>
        /// Combines source selector Func{TEntity,TProperty}(x => x.Id), and target selector (Func{TEntity,TProperty}(x => x.Identifier))
        /// into mapper action Action{TSource, TTarget}({ target.Identifier = source.Id })
        /// </summary>
        private Expression<Action<TSource, TTarget>> GetMapperExpression(MemberExpression sourcePropertyExpression, MemberExpression targetPropertyExpression)
        {
            var sourceType = typeof(TSource);
            var sourceTypeParameter = Expression.Parameter(sourceType);
            var sourceProperty = sourcePropertyExpression.Member;

            var targetType = typeof(TTarget);
            var targetTypeParameter = Expression.Parameter(targetType);
            var targetProperty = targetPropertyExpression.Member;

            var body = Expression.Assign(Expression.MakeMemberAccess(targetTypeParameter, targetProperty), Expression.MakeMemberAccess(sourceTypeParameter, sourceProperty));
            return Expression.Lambda<Action<TSource, TTarget>>(body, sourceTypeParameter, targetTypeParameter);
        }

        public SourceMapperActionType ActionType => SourceMapperActionType.Property;
        public Action<TSource, TTarget> Apply { get; }
    }
}
