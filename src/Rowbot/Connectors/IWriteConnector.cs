using Rowbot.Entities;

namespace Rowbot;

/// <summary>
/// <para>
/// Connector that supports inserting, updating, and finding data.
/// </para>
/// </summary>
/// <typeparam name="TInput">Data type to load</typeparam>
public interface IWriteConnector<TInput>
{
    /// <summary>
    /// <para>
    /// Queries the data target for a collection of entities by comparing one or more, or all fields.
    /// </para>
    /// <para>
    /// Example: Look up key by id <br/>
    /// FindAsync(entitiesToFind, compare => compare.Include(x => x.Id), result => result.Include(x => x.Key));
    /// </para>
    /// </summary>
    /// <param name="findEntities">Entities (or partial entities if comparing specific fields) to find</param>
    /// <param name="compareFieldsSelector">Fields (one or more) to compare</param>
    /// <param name="resultFieldsSelector">Fields (one or more) to return in the resulting entity if a match is found</param>
    /// <returns>A collection of entities found at the data target</returns>
    Task<IEnumerable<TInput>> FindAsync(
        IEnumerable<TInput> findEntities,
        Action<IFieldSelector<TInput>> compareFieldsSelector,
        Action<IFieldSelector<TInput>> resultFieldsSelector);
    /// <summary>
    /// Applies inserts to the data target.
    /// </summary>
    /// <param name="data">Collection of inserts to apply</param>
    /// <returns>Total rows affected</returns>
    Task<int> InsertAsync(IEnumerable<TInput> data);
    /// <summary>
    /// Applies updates to the data target.
    /// </summary>
    /// <param name="data">Collection of updates to apply</param>
    /// <returns>Total rows affected</returns>
    Task<int> UpdateAsync(IEnumerable<RowUpdate<TInput>> data);
}
