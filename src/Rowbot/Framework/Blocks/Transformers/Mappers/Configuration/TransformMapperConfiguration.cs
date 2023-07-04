using Rowbot.Common;
using Rowbot.Framework.Blocks.Transformers.Mappers.Actions;
using Rowbot.Framework.Blocks.Transformers.Mappers.Transforms;
using System.Linq.Expressions;

namespace Rowbot.Framework.Blocks.Transformers.Mappers.Configuration
{
    /// <summary>
    /// <para>
    /// Builds actions for transforming properties from source to target.
    /// </para>
    /// <para>
    /// A transform mapper allows copying and/or transforming a value from source property to target property.
    /// </para>
    /// </summary>
    public class TransformMapperConfiguration<TSource, TTarget>
    {
        private readonly MapperConfiguration<TSource, TTarget> _configuration;
        private readonly Action<ISourceMapperAction<TSource, TTarget>> _addSourceMapper;
        private readonly Action<ITargetMapperAction<TTarget>> _addTargetMapper;

        /// <param name="configuration">Reference to mapper configuration</param>
        /// <param name="addMapper">Callback to mapper configuration to add new mapper to collection</param>
        internal TransformMapperConfiguration(MapperConfiguration<TSource, TTarget> configuration, Action<ISourceMapperAction<TSource, TTarget>> addSourceMapper, Action<ITargetMapperAction<TTarget>> addTargetMapper)
        {
            _configuration = Ensure.ArgumentIsNotNull(configuration);
            _addSourceMapper = Ensure.ArgumentIsNotNull(addSourceMapper);
            _addTargetMapper = Ensure.ArgumentIsNotNull(addTargetMapper);
        }

        /// <summary>
        /// Add a custom source transform action
        /// </summary>
        public MapperConfiguration<TSource, TTarget> AddSource(ISourceMapperAction<TSource, TTarget> mapper)
        {
            _addSourceMapper(Ensure.ArgumentIsNotNull(mapper));

            return _configuration;
        }

        /// <summary>
        /// Add a custom target transform action
        /// </summary>
        public MapperConfiguration<TSource, TTarget> AddTarget(ITargetMapperAction<TTarget> mapper)
        {
            _addTargetMapper(Ensure.ArgumentIsNotNull(mapper));

            return _configuration;
        }

        /// <summary>
        /// Add a custom source action
        /// </summary>
        public MapperConfiguration<TSource, TTarget> AddSource(Action<TSource, TTarget> action)
        {
            var mapper = new SourceTransformAction<TSource, TTarget>(action);
            AddSource(mapper);

            return _configuration;
        }

        /// <summary>
        /// Built-in transform action to create a hash code from one or more properties
        /// </summary>
        public MapperConfiguration<TSource, TTarget> ToHashCode(Action<IHashCodeTransform<TTarget>> hashCodeTransform, Expression<Func<TTarget, byte[]>> targetPropertySelector)
        {
            Ensure.ArgumentIsMemberExpression(targetPropertySelector);

            var mapper = new HashCodeTransformAction<TTarget>(hashCodeTransform, targetPropertySelector);
            AddTarget(mapper);

            return _configuration;
        }
    }
}
