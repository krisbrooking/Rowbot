using Rowbot.Framework.Blocks.Loaders;
using Rowbot.Framework.Pipelines.Options;

namespace Rowbot
{
    /// <summary>
    /// <para>
    /// The loader extends the functionality of the write connector.
    /// </para>
    /// <para>
    /// Note: This interface is intended for use by the load block. Do not implement this interface when creating a loader, implement <see cref="ILoader{TTarget, TOptions}"/> instead.
    /// </para>
    /// </summary>
    /// <typeparam name="TTarget">Target entity</typeparam>
    public interface ILoader<TTarget>
    {
        /// <summary>
        /// Loads data. Orchestrates a write connector's find, insert, and update operations.
        /// </summary>
        /// <param name="data">Array of entities to load</param>
        /// <returns>Load result containing entities inserted and updated</returns>
        Task<LoadResult<TTarget>> LoadAsync(TTarget[] data);
    }

    /// <summary>
    /// <para>
    /// The loader extends the functionality of the write connector.
    /// </para>
    /// </summary>
    /// <typeparam name="TTarget">Target entity</typeparam>
    /// <typeparam name="TOptions">Loader options type</typeparam>
    public interface ILoader<TTarget, TOptions> : ILoader<TTarget>
        where TOptions : LoaderOptions
    {
        /// <summary>
        /// Loader options. Used to provide configuration from pipeline builder to the loader.
        /// </summary>
        TOptions Options { get; set; }
        /// <summary>
        /// The write connector to be extended.
        /// </summary>
        IWriteConnector<TTarget> Connector { get; set; }
    }
}
