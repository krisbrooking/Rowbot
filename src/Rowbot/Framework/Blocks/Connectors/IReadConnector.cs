namespace Rowbot
{
    /// <summary>
    /// <para>
    /// Connector that supports querying data.
    /// </para>
    /// <para>
    /// Note: This interface is intended for use by extractors. Do not implement this interface when creating a read connector, implement <see cref="IReadConnector{TSource, TOptions}"/> instead.
    /// </para>
    /// </summary>
    /// <typeparam name="TSource">Source entity</typeparam>
    public interface IReadConnector<TSource>
    {
        /// <summary>
        /// Performs a query operation on a data source.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns>Returned result set</returns>
        Task<IEnumerable<TSource>> QueryAsync(IEnumerable<ExtractParameter> parameters);
    }

    /// <summary>
    /// <para>
    /// Connector that supports querying data.
    /// </para>
    /// </summary>
    /// <typeparam name="TSource">Source entity</typeparam>
    /// <typeparam name="TOptions">Read connector options type</typeparam>
    public interface IReadConnector<TSource, TOptions> : IReadConnector<TSource>
    {
        /// <summary>
        /// Read connector options. Used to provide configuration from pipeline builder to the read connector.
        /// </summary>
        TOptions Options { get; set; }
    }
}
