namespace Rowbot;

/// <summary>
/// <para>
/// Connector that supports creating a dataset.
/// </para>
/// </summary>
public interface ICreateConnector
{
    /// <summary>
    /// Creates a data set.
    /// </summary>
    /// <returns>Success status</returns>
    Task<bool> CreateDataSetAsync();
}