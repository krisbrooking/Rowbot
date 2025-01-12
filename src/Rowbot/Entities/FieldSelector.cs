using Rowbot.Common;
using System.Linq.Expressions;

namespace Rowbot.Entities;

/// <summary>
/// <para>
/// Builder for selecting one or more fields of an entity.
/// </para>
/// <para>
/// Use <see cref="EntityAccessor{TEntity}.EntityAccessor(IFieldSelection{TEntity})"/> to
/// access the values of the selected fields.
/// </para>
/// </summary>
public interface IFieldSelector<TEntity> : IFieldSelection<TEntity>
{
    /// <inheritdoc cref="FieldSelector{TEntity}.Include{TField}(Expression{Func{TEntity, TField}})" />
    ISingleFieldSelector<TEntity> Include<TField>(Expression<Func<TEntity, TField>> fieldSelector);
    /// <inheritdoc cref="FieldSelector{TEntity}.All" />
    IFieldSelection<TEntity> All();
}

public interface ISingleFieldSelector<TEntity> : IFieldSelection<TEntity>
{
    /// <inheritdoc cref="FieldSelector{TEntity}.Include{TField}(Expression{Func{TEntity, TField}})" />
    ISingleFieldSelector<TEntity> Include<TField>(Expression<Func<TEntity, TField>> fieldSelector);
}

public interface IFieldSelection<TEntity>
{
    /// <summary>
    /// Collection of selected fields
    /// </summary>
    IEnumerable<FieldDescriptor> Selected { get; }
}

/// <inheritdoc cref="IFieldSelector{TEntity}"/>
public sealed class FieldSelector<TEntity> : IFieldSelector<TEntity>, ISingleFieldSelector<TEntity>, IFieldSelection<TEntity>
{
    internal List<FieldDescriptor> _fieldDescriptors;

    /// <inheritdoc cref="IFieldSelector{TEntity}"/>
    public FieldSelector()
    {
        _fieldDescriptors = new List<FieldDescriptor>();
    }

    /// <inheritdoc cref="IFieldSelector{TEntity}"/>
    public static IFieldSelector<TEntity> Create() => new FieldSelector<TEntity>();

    public IEnumerable<FieldDescriptor> Selected => _fieldDescriptors;

    /// <summary>
    /// Selects single field
    /// </summary>
    public ISingleFieldSelector<TEntity> Include<TField>(Expression<Func<TEntity, TField>> fieldSelector)
    {
        var memberExpression = Ensure.ArgumentIsMemberExpression(fieldSelector);
        var property = Ensure.MemberExpressionTargetsProperty(memberExpression);

        if (!_fieldDescriptors.Any(x => string.Equals(x.Property.Name, memberExpression.Member.Name, StringComparison.OrdinalIgnoreCase)))
        {
            var field = new FieldDescriptor(property);

            _fieldDescriptors.Add(field);
        }

        return this;
    }

    /// <summary>
    /// Selects all fields in <typeparamref name="TEntity"/>
    /// </summary>
    public IFieldSelection<TEntity> All()
    {
        _fieldDescriptors = new EntityDescriptor<TEntity>().Fields.ToList();

        return this;
    }
}