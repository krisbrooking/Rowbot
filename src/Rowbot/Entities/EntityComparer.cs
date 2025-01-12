using System.Linq.Expressions;
using System.Reflection;

namespace Rowbot.Entities;

/// <summary>
/// <para>
/// Compares the property values of two entity instances.
/// </para>
/// <para>
/// Supports <see cref="object.Equals"/> for primitive types, <see cref="Enumerable.SequenceEqual"/> for array types
/// and <see cref="EqualityComparer{T}.Default.Equals"/> for anything else.
/// </para>
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public sealed class EntityComparer<TEntity>
{
    private readonly EntityDescriptor<TEntity> _entityDescriptor;
    private readonly Dictionary<string, Func<TEntity, TEntity, bool>> _comparers;

    /// <inheritdoc cref="EntityComparer{TEntity}"/>
    public EntityComparer(EntityDescriptor<TEntity> entityDescriptor)
    {
        _entityDescriptor = entityDescriptor;
        _comparers = BuildComparers(_entityDescriptor.Fields);
    }

    /// <summary>
    /// Determines whether a single property of two objects of type <typeparamref name="TEntity"/> are equal
    /// </summary>
    /// <param name="fieldDescriptor">The property to compare</param>
    public bool FieldEquals(FieldDescriptor fieldDescriptor, TEntity left, TEntity right)
    {
        var comparer = _comparers[fieldDescriptor.Property.Name];
        return comparer(left, right);
    }

    /// <summary>
    /// Determines whether two objects of type <typeparamref name="TEntity"/> are equal and returns a list of properties with different values
    /// </summary>
    public EntityEqualityResult EntityEquals(TEntity left, TEntity right)
    {
        var differentPropertyValues = new List<string>();
        var isEqual = true;

        foreach (var comparer in _comparers)
        {
            if (!comparer.Value(left, right))
            {
                isEqual = false;
                differentPropertyValues.Add(comparer.Key);
            }
        }

        return new EntityEqualityResult(isEqual, differentPropertyValues);
    }

    public Func<TEntity, TEntity, bool> GetFieldComparer(FieldDescriptor fieldDescriptor)
    {
        if (!_comparers.ContainsKey(fieldDescriptor.Property.Name))
        {
            return BuildFieldComparer(fieldDescriptor);
        }

        return _comparers[fieldDescriptor.Property.Name];
    }

    private Dictionary<string, Func<TEntity, TEntity, bool>> BuildComparers(IEnumerable<FieldDescriptor> fieldDescriptors)
    {

        var result = new Dictionary<string, Func<TEntity, TEntity, bool>>();

        foreach (var fieldDescriptor in fieldDescriptors)
        {
            if (result.ContainsKey(fieldDescriptor.Property.Name))
            {
                continue;
            }

            result.Add(fieldDescriptor.Property.Name, BuildFieldComparer(fieldDescriptor));
        }

        return result;
    }

    private Func<TEntity, TEntity, bool> BuildFieldComparer(FieldDescriptor fieldDescriptor)
    {
        if (fieldDescriptor.Property.PropertyType.IsPrimitive ||
                fieldDescriptor.Property.PropertyType == typeof(string))
        {
            return BuildPrimitiveComparer(fieldDescriptor.Property);
        }
        else if (fieldDescriptor.Property.PropertyType.IsArray)
        {
            return BuildArrayComparer(fieldDescriptor.Property);
        }
        else
        {
            return BuildDefaultComparer(fieldDescriptor.Property);
        }
    }

    /// <summary>
    /// Builds (source, target) => source.Id.Equals(target.Id)
    /// </summary>
    private Func<TEntity, TEntity, bool> BuildPrimitiveComparer(PropertyInfo property)
    {
        var equalsMethodInfo = property.PropertyType.GetMethod("Equals", new Type[] { property.PropertyType })!;

        var entityType = typeof(TEntity);
        var leftParameter = Expression.Parameter(entityType, "source");
        var rightParameter = Expression.Parameter(entityType, "target");

        var leftMemberAccess = Expression.MakeMemberAccess(leftParameter, property);
        var rightMemberAccess = Expression.MakeMemberAccess(rightParameter, property);

        var body = Expression.Call(leftMemberAccess, equalsMethodInfo, rightMemberAccess);

        if (property.PropertyType == typeof(string) || Nullable.GetUnderlyingType(property.PropertyType) != null)
        {
            var leftNullCheck = Expression.Equal(leftMemberAccess, Expression.Constant(null, property.PropertyType));
            var leftNotNullCheck = Expression.NotEqual(leftMemberAccess, Expression.Constant(null, property.PropertyType));
            var rightNullCheck = Expression.Equal(rightMemberAccess, Expression.Constant(null, property.PropertyType));

            var lambda = Expression.Lambda<Func<TEntity, TEntity, bool>>(Expression.OrElse(Expression.AndAlso(leftNullCheck, rightNullCheck), Expression.AndAlso(leftNotNullCheck, body)), leftParameter, rightParameter);
            return lambda.Compile();
        }
        else
        {
            var lambda = Expression.Lambda<Func<TEntity, TEntity, bool>>(body, leftParameter, rightParameter);
            return lambda.Compile();
        }
    }

    /// <summary>
    /// Builds (source, target) => Enumerable{T}.SequenceEqual(source.Id, target.Id)
    /// </summary>
    private Func<TEntity, TEntity, bool> BuildArrayComparer(PropertyInfo property)
    {
        var genericEnumerableType = typeof(IEnumerable<>).MakeGenericType(property.PropertyType.GetElementType()!);
        var equalsMethodInfo = typeof(Enumerable).GetMethod(nameof(Enumerable.SequenceEqual), new Type[] { genericEnumerableType, genericEnumerableType })!;

        var entityType = typeof(TEntity);
        var leftParameter = Expression.Parameter(entityType, "source");
        var rightParameter = Expression.Parameter(entityType, "target");

        var leftMemberAccess = Expression.MakeMemberAccess(leftParameter, property);
        var rightMemberAccess = Expression.MakeMemberAccess(rightParameter, property);

        var leftNullCheck = Expression.NotEqual(leftMemberAccess, Expression.Constant(null, typeof(object)));
        var rightNullCheck = Expression.NotEqual(rightMemberAccess, Expression.Constant(null, typeof(object)));

        var body = Expression.Call(typeof(Enumerable), nameof(Enumerable.SequenceEqual), new Type[] { property.PropertyType.GetElementType()! }, leftMemberAccess, rightMemberAccess);

        var lambda = Expression.Lambda<Func<TEntity, TEntity, bool>>(Expression.AndAlso(leftNullCheck, Expression.AndAlso(rightNullCheck, body)), leftParameter, rightParameter);

        return lambda.Compile();
    }

    /// <summary>
    /// Builds (source, target) => EqualityComparer{T}.Default.Equals(source.Id, target.Id)
    /// </summary>
    private Func<TEntity, TEntity, bool> BuildDefaultComparer(PropertyInfo property)
    {
        var type = property.PropertyType;

        var equalityComparerDefaultInstance = typeof(EqualityComparer<>)
                .MakeGenericType(type)
                .GetProperty("Default", BindingFlags.Public | BindingFlags.Static)!
                .GetValue(null)!;

        var equalsMethodInfo = equalityComparerDefaultInstance.GetType()
            .GetMethod("Equals", new Type[] { type, type })!;

        var entityType = typeof(TEntity);
        var sourceParameter = Expression.Parameter(entityType, "source");
        var targetParameter = Expression.Parameter(entityType, "target");

        var sourceMemberAccess = Expression.MakeMemberAccess(sourceParameter, property);
        var targetMemberAccess = Expression.MakeMemberAccess(targetParameter, property);

        var body = Expression.Call(Expression.Constant(equalityComparerDefaultInstance), equalsMethodInfo, sourceMemberAccess, targetMemberAccess);

        var lambda = Expression.Lambda<Func<TEntity, TEntity, bool>>(body, sourceParameter, targetParameter);
        return lambda.Compile();
    }

    public record EntityEqualityResult(bool IsEqual, IEnumerable<string> DifferentPropertyValues);
}
