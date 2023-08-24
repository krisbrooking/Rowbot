using Rowbot.Framework.Pipelines.Options;

namespace Rowbot
{
    /// <summary>
    /// <para>
    /// The extractor extends the functionality of the read connector.
    /// </para>
    /// <para>
    /// Note: This interface is intended for use by the extract block. Do not implement this interface when creating an extractor, implement <see cref="IExtractor{TSource, TOptions}"/> instead.
    /// </para>
    /// </summary>
    /// <typeparam name="TSource">Source entity</typeparam>
    public interface IExtractor<TSource>
    {
        /// <summary>
        /// <para>
        /// Extracts data. Extends the read connector's query operation by injecting extract parameters.
        /// </para>
        /// <para>
        /// The framework calls the extract method for every collection of extract parameters provided by the user.
        /// </para>
        /// </summary>
        /// <param name="userDefinedParameters">Extract parameters provided by the user</param>
        /// <returns>Entity iterator</returns>
        IAsyncEnumerable<TSource> ExtractAsync(ExtractParameterCollection userDefinedParameters, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// <para>
    /// The extractor extends the functionality of the read connector.
    /// </para>
    /// </summary>
    /// <typeparam name="TSource">Source entity</typeparam>
    /// <typeparam name="TOptions">Extractor options type</typeparam>
    public interface IExtractor<TSource, TOptions> : IExtractor<TSource>
        where TOptions : ExtractorOptions
    {
        /// <summary>
        /// Extractor options. Used to provide configuration from pipeline builder to the extractor.
        /// </summary>
        TOptions Options { get; set; }
        /// <summary>
        /// The read connector to be extended.
        /// </summary>
        IReadConnector<TSource> Connector { get; set; }
    }
}
