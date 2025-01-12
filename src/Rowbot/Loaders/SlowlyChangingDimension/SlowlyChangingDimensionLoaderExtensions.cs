using Rowbot.Loaders.SlowlyChangingDimension;

namespace Rowbot;

public static class SlowlyChangingDimensionLoaderExtensions
{
    /// <summary>
    /// <para>
    /// Inserts and updates rows using a write connector and includes support for change history.
    /// </para>
    /// <para>
    /// Slowly changing dimension loader updates data in one of two ways.<br/>
    /// Type 1 updates are made in place using the row that already exists.<br/>
    /// Type 2 updates are made by changing the status of the current row to inactive and creating a new row with any changes.<br/>
    /// </para>
    /// <para>
    /// The <see cref="Row.KeyHash"/> property is used to determine whether a row already exists.<br/>
    /// The <see cref="Row.ChangeHash"/> property is used to determine whether any value of a row has changed.
    /// </para>
    /// </summary>
    public static ILoadBuilderLoaderStep<TInput> WithSlowlyChangingDimension<TInput, TConnector>(
        this ILoadBuilderConnectorStep<TInput, TConnector> connectorStep, 
        Action<SlowlyChangingDimensionLoaderOptions<TInput>>? configure = null)
        where TInput : Dimension
        where TConnector : IWriteConnector<TInput>
    {
        var options = new SlowlyChangingDimensionLoaderOptions<TInput>();
        configure?.Invoke(options);

        return connectorStep.WithLoader<SlowlyChangingDimensionLoader<TInput>>(loader => loader.Options = options);
    }
}
