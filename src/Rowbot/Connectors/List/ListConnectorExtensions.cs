using Rowbot.Connectors.List;

namespace Rowbot;

public static class ListConnectorExtensions
{
    /// <summary>
    /// Extracts data from an in-memory list.
    /// </summary>
    public static IExtractBuilderConnectorStep<TInput, TOutput> FromList<TInput, TOutput>(
        this IExtractBuilder<TInput, TOutput> builder, 
        IEnumerable<TOutput> data)
    {
        var options = new ListReadConnectorOptions<TOutput>();
        options.Query = (parameters) => data;

        return builder.WithConnector<ListReadConnector<TInput, TOutput>>(connector => connector.Options = options);
    }
        
    /// <summary>
    /// Extracts data from an in-memory list.
    /// </summary>
    public static IExtractBuilderConnectorStep<TInput, TOutput> FromList<TInput, TOutput>(
        this IExtractBuilder<TInput, TOutput> builder, 
        Func<ExtractParameter[], IEnumerable<TOutput>> query)
    {
        var options = new ListReadConnectorOptions<TOutput>();
        options.Query = query;

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
    public Func<ExtractParameter[], IEnumerable<TOutput>> Query { get; set; } = (parameters) => [];
}

public sealed class ListWriteConnectorOptions<TInput>
{
    public List<TInput> Target { get; set; } = new();
}