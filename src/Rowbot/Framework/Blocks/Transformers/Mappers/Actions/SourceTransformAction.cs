using System.Linq.Expressions;

namespace Rowbot.Framework.Blocks.Transformers.Mappers.Actions
{
    /// <summary>
    /// Wraps a mapper action expression in an <see cref="ISourceMapperAction{TSource, TTarget}"/> interface.
    /// For use when extending <see cref="MapperConfiguration{TSource, TTarget}"/> from an extension method.
    /// </summary>
    public sealed class SourceTransformAction<TSource, TTarget> : ISourceMapperAction<TSource, TTarget>
    {
        /// <inheritdoc cref="SourceTransformAction{TSource, TTarget}"/>
        public SourceTransformAction(Expression<Action<TSource, TTarget>> mapperExpression)
        {
            Apply = mapperExpression.Compile();
        }

        /// <inheritdoc cref="SourceTransformAction{TSource, TTarget}"/>
        public SourceTransformAction(Action<TSource, TTarget> action)
        {
            Apply = action;
        }

        public SourceMapperActionType ActionType => SourceMapperActionType.Transform;
        public Action<TSource, TTarget> Apply { get; }
    }
}
