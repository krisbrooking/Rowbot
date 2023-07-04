using Rowbot.Framework.Blocks.Transformers.Mappers.Actions;
using Rowbot.Common;
using System.Linq.Expressions;

namespace Rowbot.Framework.Blocks.Transformers.Mappers.Configuration
{
    /// <summary>
    /// <para>
    /// Builds actions for mapper properties from source to target.
    /// </para>
    /// <para>
    /// A property mapper allows copying values from source property to target property.
    /// </para>
    /// </summary>
    public class PropertyMapperConfiguration<TSource, TTarget>
    {
        private readonly MapperConfiguration<TSource, TTarget> _configuration;
        private readonly Action<ISourceMapperAction<TSource, TTarget>> _addMapper;

        /// <param name="configuration">Reference to mapper configuration</param>
        /// <param name="addMapper">Callback to mapper configuration to add new mapper to collection</param>
        internal PropertyMapperConfiguration(MapperConfiguration<TSource, TTarget> configuration, Action<ISourceMapperAction<TSource, TTarget>> addMapper)
        {
            _configuration = Ensure.ArgumentIsNotNull(configuration);
            _addMapper = Ensure.ArgumentIsNotNull(addMapper);
        }

        /// <summary>
        /// <para>
        /// Map from source to target property. Example: source => source.Id, target => target.Identifier
        /// </para>
        /// <para>
        /// Source and target property types must match.
        /// </para>
        /// </summary>
        public MapperConfiguration<TSource, TTarget> Property<TProperty>(Expression<Func<TSource, TProperty>> sourcePropertySelector, Expression<Func<TTarget, TProperty>> targetPropertySelector)
        {
            MemberExpression sourcePropertyExpression = Ensure.ArgumentIsMemberExpression(sourcePropertySelector);
            MemberExpression targetPropertyExpression = Ensure.ArgumentIsMemberExpression(targetPropertySelector);

            Ensure.SelectorsTargetTheSamePropertyType(sourcePropertyExpression, targetPropertyExpression);

            var mapper = new PropertyMapperAction<TSource, TTarget, TProperty>(sourcePropertyExpression, targetPropertyExpression);
            Add(mapper);

            return _configuration;
        }

        private MapperConfiguration<TSource, TTarget> Add(ISourceMapperAction<TSource, TTarget> mapper)
        {
            _addMapper(Ensure.ArgumentIsNotNull(mapper));

            return _configuration;
        }
    }
}
