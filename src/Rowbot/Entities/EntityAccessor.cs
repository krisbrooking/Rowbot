using Rowbot.Common;
using System.Linq.Expressions;

namespace Rowbot.Entities
{
    /// <summary>
    /// Provides accessors for getting or setting field values
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public sealed class EntityAccessor<TEntity>
    {
        private readonly IEnumerable<FieldDescriptor> _fieldDescriptors;
        private readonly Dictionary<string, Action<TEntity, TEntity>> _fieldMappers;
        private readonly Dictionary<string, Action<object, TEntity>> _valueMappers;
        private readonly Dictionary<string, Func<TEntity, object>> _valueAccessors;

        private EntityAccessor(IEnumerable<FieldDescriptor> fieldDescriptors)
        {
            _fieldDescriptors = fieldDescriptors;
            _fieldMappers = new Dictionary<string, Action<TEntity, TEntity>>();
            _valueMappers = new Dictionary<string, Action<object, TEntity>>();
            _valueAccessors = new Dictionary<string, Func<TEntity, object>>();
        }

        /// <inheritdoc cref="EntityAccessor{TEntity}"/>
        public EntityAccessor(EntityDescriptor<TEntity> entityDescriptor) : this(entityDescriptor.Fields) { }

        /// <inheritdoc cref="EntityAccessor{TEntity}"/>
        public EntityAccessor(IFieldSelection<TEntity> fieldSelector) : this(fieldSelector.Selected) { }

        public IEnumerable<FieldDescriptor> Fields => _fieldDescriptors;

        /// <summary>
        /// Gets a field mapper delegate. 
        /// Invoke with source entity as the first argument: Action{Source, Target}
        /// </summary>
        public Action<TEntity, TEntity> GetFieldMapper(FieldDescriptor fieldDescriptor)
        {
            FieldDescriptor field = Ensure.ItemExistsInCollection(_fieldDescriptors, x => string.Equals(x.Property.Name, fieldDescriptor.Property.Name, StringComparison.OrdinalIgnoreCase), fieldDescriptor.Property.Name);

            if (_fieldMappers.ContainsKey(fieldDescriptor.Property.Name))
            {
                return _fieldMappers[fieldDescriptor.Property.Name];
            }

            var mapper = BuildFieldMapperExpression(field).Compile();
            _fieldMappers.Add(field.Property.Name, mapper);

            return mapper;
        }

        /// <summary>
        /// Gets a value mapper delegate. 
        /// Invoke with source entity as the first argument: Action{object, Target}
        /// </summary>
        public Action<object, TEntity> GetValueMapper(FieldDescriptor fieldDescriptor)
        {
            FieldDescriptor field = Ensure.ItemExistsInCollection(_fieldDescriptors, x => string.Equals(x.Property.Name, fieldDescriptor.Property.Name, StringComparison.OrdinalIgnoreCase), fieldDescriptor.Property.Name);

            if (_valueMappers.ContainsKey(fieldDescriptor.Property.Name))
            {
                return _valueMappers[fieldDescriptor.Property.Name];
            }

            var mapper = BuildValueMapperExpression(field).Compile();
            _valueMappers.Add(field.Property.Name, mapper);

            return mapper;
        }

        /// <summary>
        /// Gets a value accessor delegate.
        /// </summary>
        public Func<TEntity, object> GetValueAccessor(FieldDescriptor fieldDescriptor)
        {
            FieldDescriptor field = Ensure.ItemExistsInCollection(_fieldDescriptors, x => string.Equals(x.Property.Name, fieldDescriptor.Property.Name, StringComparison.OrdinalIgnoreCase), fieldDescriptor.Property.Name);

            if (_valueAccessors.ContainsKey(fieldDescriptor.Property.Name))
            {
                return _valueAccessors[fieldDescriptor.Property.Name];
            }

            var mapper = BuildValueAccessorExpression(field).Compile();
            _valueAccessors.Add(field.Property.Name, mapper);

            return mapper;
        }

        /// <summary>
        /// Builds a field mapper delegate expression. 
        /// Invoke with source entity as the first argument: Action{Source, Target}
        /// </summary>
        public static Expression<Action<TEntity, TEntity>> BuildFieldMapperExpression(FieldDescriptor field)
        {
            var entityType = typeof(TEntity);
            var sourceTypeParameter = Expression.Parameter(entityType, "source");
            var targetTypeParameter = Expression.Parameter(entityType, "target");

            var body = Expression.Assign(Expression.MakeMemberAccess(targetTypeParameter, field.Property), Expression.MakeMemberAccess(sourceTypeParameter, field.Property));

            return Expression.Lambda<Action<TEntity, TEntity>>(body, sourceTypeParameter, targetTypeParameter);
        }

        /// <summary>
        /// Builds a value mapper expression.
        /// </summary>
        public static Expression<Action<object, TEntity>> BuildValueMapperExpression(FieldDescriptor field)
        {
            var sourceTypeParameter = Expression.Parameter(typeof(object), "source");
            var targetTypeParameter = Expression.Parameter(typeof(TEntity), "target");

            if (field.Property.CanWrite)
            {
                var body = Expression.Assign(Expression.MakeMemberAccess(targetTypeParameter, field.Property), Expression.Convert(sourceTypeParameter, field.Property.PropertyType));

                return Expression.Lambda<Action<object, TEntity>>(body, sourceTypeParameter, targetTypeParameter);
            }

            var lambda = Expression.Lambda<Action<object, TEntity>>(Expression.Empty(), sourceTypeParameter, targetTypeParameter);
            return lambda;
        }

        /// <summary>
        /// Builds a value accessor expression.
        /// </summary>
        public static Expression<Func<TEntity, object>> BuildValueAccessorExpression(FieldDescriptor field)
        {
            var entityType = typeof(TEntity);
            var entityTypeParameter = Expression.Parameter(entityType, "x");

            var body = Expression.Convert(Expression.MakeMemberAccess(entityTypeParameter, field.Property), typeof(object));

            return Expression.Lambda<Func<TEntity, object>>(body, entityTypeParameter);
        }
    }
}
