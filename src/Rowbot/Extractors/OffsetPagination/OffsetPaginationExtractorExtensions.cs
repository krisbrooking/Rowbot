using Rowbot.Extractors.OffsetPagination;

namespace Rowbot;

public static class OffsetPaginationExtractorExtensions
{
    /// <summary>
    /// <para>
    /// Adds the offset pagination extractor which generates query parameters for batch size and next offset for each query executed by the connector.
    /// </para>
    /// <para>
    /// This extractor generates two extract parameters that must be included in your query.<br />
    /// 1. Batch size. The @BatchSize parameter defaults to 1000.<br/>
    /// 2. Offset. The @Offset parameter is set to the start of the next page.
    /// </para>
    /// <para>
    /// How to use with a SQL query:<br />
    /// SELECT * FROM [Table] <br/>ORDER BY [Id]<br/>OFFSET @Offset <br/>LIMIT @BatchSize
    /// </para>
    /// </summary>
    public static IExtractBuilderExtractorStep<TInput, TOutput> WithOffsetPagination<TInput, TOutput>(
        this IExtractBuilderConnectorStep<TInput, TOutput> connectorStep, 
        Action<OffsetPaginationOptions>? configure = null)
    {
        var options = new OffsetPaginationOptions();
        configure?.Invoke(options);

        return connectorStep.WithExtractor<OffsetPaginationExtractor<TInput, TOutput>>(extractor => extractor.Options = options);
    }
}

public class OffsetPaginationOptions
{
    public int InitialValue { get; set; } = 0;
    public OffsetOrder OrderBy { get; set; } = OffsetOrder.Ascending;
}
