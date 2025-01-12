using Rowbot.Extractors.Framework;

namespace Rowbot;

/// <summary>
/// <para>
/// The extractor extends the functionality of the read connector.
/// </para>
/// </summary>
/// <typeparam name="TInput">Data type of previous extractor. Used as context for the current extractor</typeparam>
/// <typeparam name="TOutput">Data type to extract</typeparam>
public interface IExtractor<TInput, TOutput>
{
    /// <summary>
    /// <para>
    /// Extracts data. Extends the read connector's query operation by injecting extract parameters.
    /// </para>
    /// </summary>
    /// <returns>Entity iterator</returns>
    IAsyncEnumerable<TOutput> ExtractAsync(ExtractContext<TInput> context, CancellationToken cancellationToken = default);
    /// <summary>
    /// The read connector to be extended.
    /// </summary>
    IReadConnector<TInput, TOutput>? Connector { get; set; }
}