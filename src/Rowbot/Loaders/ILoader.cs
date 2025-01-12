using Rowbot.Loaders.Framework;

namespace Rowbot;

/// <summary>
/// <para>
/// The loader extends the functionality of the write connector.
/// </para>
/// </summary>
/// <typeparam name="TInput">Target entity</typeparam>
public interface ILoader<TInput>
{
    /// <summary>
    /// Loads data. Orchestrates a write connector's find, insert, and update operations.
    /// </summary>
    /// <param name="data">Array of entities to load</param>
    /// <returns>Load result containing entities inserted and updated</returns>
    Task<LoadResult<TInput>> LoadAsync(TInput[] data);
    /// <summary>
    /// The write connector to be extended.
    /// </summary>
    IWriteConnector<TInput>? Connector { get; set; }
}