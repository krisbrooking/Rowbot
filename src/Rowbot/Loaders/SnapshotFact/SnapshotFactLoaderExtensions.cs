﻿using Rowbot.Loaders.SnapshotFact;

namespace Rowbot;

public static class SnapshotFactLoaderExtensions
{
    /// <summary>
    /// <para>
    /// Inserts and updates rows using a write connector.
    /// </para>
    /// <para>
    /// Snapshot fact loader does not insert a row that already exists.
    /// </para>
    /// <para>
    /// The <see cref="Row.KeyHash"/> property is used to determine whether a row already exists.<br/>
    /// The <see cref="Row.ChangeHash"/> property is used to determine whether any value of a row has changed.
    /// </para>
    /// </summary>
    public static ILoadBuilderLoaderStep<TInput> WithSnapshotFact<TInput, TConnector>(
        this ILoadBuilderConnectorStep<TInput, TConnector> connectorStep)
        where TInput : Fact
        where TConnector : IWriteConnector<TInput>
    {
        return connectorStep.WithLoader<SnapshotFactLoader<TInput>>();
    }
}