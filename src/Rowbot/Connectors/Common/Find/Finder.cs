using Rowbot.Entities;

namespace Rowbot.Connectors.Common.Find;

internal interface IFinder { }

/// <summary>
/// Provides helper methods for simple implementations of <see cref="IWriteConnector{TEntity}.FindAsync(IEnumerable{TEntity}, Action{IFieldSelector{TEntity}}, Action{IFieldSelector{TEntity}})"/>
/// including predicate for comparison and mappers for result properties.
/// </summary>
public sealed class Finder<TEntity> : IFinder
{
    internal readonly IList<Func<TEntity, TEntity, bool>> _comparers;
    internal readonly IList<Action<TEntity, TEntity>> _mappers;

    /// <inheritdoc cref="Finder{TEntity}"/>
    public Finder(IFieldSelection<TEntity> compareSelector, IFieldSelection<TEntity> resultSelector, EntityComparer<TEntity> entityComparer)
    {
        _comparers = new List<Func<TEntity, TEntity, bool>>();
        _mappers = new List<Action<TEntity, TEntity>>();

        foreach (var compareField in compareSelector.Selected)
        {
            _comparers.Add(entityComparer.GetFieldComparer(compareField));
        }

        var fields = compareSelector.Selected
            .Concat(resultSelector.Selected
                .Where(x => !compareSelector.Selected
                    .Select(x => x.Property.Name)
                    .Contains(x.Property.Name)));

        foreach (var field in fields.Where(x => x.Property.GetSetMethod() != null))
        {
            _mappers.Add(EntityAccessor<TEntity>.BuildFieldMapperExpression(field).Compile());
        }
    }

    /// <summary>
    /// Invokes a comparer for every field specified in the finder. A comparer returns true
    /// if the field value of the left and right entity match exactly.
    /// </summary>
    /// <returns>True if all comparers return true</returns>
    public bool Compare(TEntity left, TEntity right)
    {
        foreach (var comparer in _comparers)
        {
            if (!comparer.Invoke(left, right))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Maps selected properties to new instance of <typeparamref name="TEntity"/>. 
    /// Invokes one or more mappers for findSelector and one or more mappers for resultSelector.
    /// </summary>
    /// <returns>Mapped <typeparamref name="TEntity"/></returns>
    public TEntity Return(TEntity source)
    {
        var result = Activator.CreateInstance<TEntity>();

        foreach (var mapper in _mappers)
        {
            mapper(source, result);
        }

        return result;
    }
}