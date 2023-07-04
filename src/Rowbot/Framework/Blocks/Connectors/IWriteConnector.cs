using Rowbot.Entities;

namespace Rowbot
{
    /// <summary>
    /// <para>
    /// Connector that supports inserting, updating, and finding data.
    /// </para>
    /// <para>
    /// Note: This interface is intended for use by loaders. Do not implement this interface when creating a write connector, implement <see cref="IWriteConnector{TTarget, TOptions}"/> instead.
    /// </para>
    /// </summary>
    /// <typeparam name="TTarget">Target entity</typeparam>
    public interface IWriteConnector<TTarget>
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
        Task<IEnumerable<TTarget>> FindAsync(
            IEnumerable<TTarget> findEntities,
            Action<IFieldSelector<TTarget>> compareFieldsSelector,
            Action<IFieldSelector<TTarget>> resultFieldsSelector);
        /// <summary>
        /// Applies inserts to the data target.
        /// </summary>
        /// <param name="data">Collection of inserts to apply</param>
        /// <returns>Total rows affected</returns>
        Task<int> InsertAsync(IEnumerable<TTarget> data);
        /// <summary>
        /// Applies updates to the data target.
        /// </summary>
        /// <param name="data">Collection of updates to apply</param>
        /// <returns>Total rows affected</returns>
        Task<int> UpdateAsync(IEnumerable<Update<TTarget>> data);
    }

    /// <summary>
    /// <para>
    /// Connector that supports inserting, updating, and finding data.
    /// </para>
    /// </summary>
    /// <typeparam name="TTarget">Target entity</typeparam>
    /// <typeparam name="TOptions">Write connector options type</typeparam>
    public interface IWriteConnector<TTarget, TOptions> : IWriteConnector<TTarget>
    {
        /// <summary>
        /// Write connector options. Used to provide configuration from pipeline builder to the write connector.
        /// </summary>
        TOptions Options { get; set; }
    }
}
