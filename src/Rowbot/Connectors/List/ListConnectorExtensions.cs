using Rowbot.Connectors.List;

namespace Rowbot;

public static class ListConnectorExtensions
{
    /// <summary>
    /// Extracts data from an in-memory list.
    /// </summary>
    public static IExtractBuilderConnectorStep<TInput, TOutput> FromList<TInput, TOutput>(
        this IExtractBuilder<TInput, TOutput> builder, 
        List<TOutput> data)
    {
        var options = new ListReadConnectorOptions<TOutput>
        {
            Data = data,
        };

        return builder.WithConnector<ListReadConnector<TInput, TOutput>>(connector => connector.Options = options);
    }

    /// <summary>
    /// Writes data to an in-memory list.
    /// </summary>
    public static ILoadBuilderConnectorStep<TInput, ListWriteConnector<TInput>> ToList<TInput>(
        this ILoadBuilder<TInput> builder,
        List<TInput> target)
    {
        var options = new ListWriteConnectorOptions<TInput>();
        options.Target = target;

        return builder.WithConnector<ListWriteConnector<TInput>>(connector => connector.Options = options);
    }
}
    
public sealed class ListReadConnectorOptions<TOutput>
{
    public List<TOutput> Data { get; set; } = [];
}

public sealed class ListWriteConnectorOptions<TInput>
{
    public List<TInput> Target { get; set; } = new();
}