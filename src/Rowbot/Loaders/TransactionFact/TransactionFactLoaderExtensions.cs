using Rowbot.Loaders.TransactionFact;

namespace Rowbot;

public static class TransactionFactLoaderExtensions
{
    /// <summary>
    /// <para>
    /// Inserts new rows using a write connector. Updating is not supported.
    /// </para>
    /// <para>
    /// Fact loader does not insert a row that already exists. The <see cref="Row.KeyHash"/> property is used to determine whether a row already exists.</para>
    /// </summary>
    public static ILoadBuilderLoaderStep<TInput> WithFact<TInput, TConnector>(
        this ILoadBuilderConnectorStep<TInput, TConnector> connectorStep)
        where TInput : Fact
        where TConnector : IWriteConnector<TInput>
    {
        return connectorStep.WithLoader<TransactionFactLoader<TInput>>();
    }
}
