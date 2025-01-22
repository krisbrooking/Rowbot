using Rowbot.Extractors.CursorPagination;
using System.Linq.Expressions;

namespace Rowbot;

public static class CursorPaginationExtractorExtensions
{
    /// <summary>
    /// <para>
    /// Adds the cursor pagination extractor which generates query parameters for batch size and next cursor for each query executed by the connector.
    /// </para>
    /// <para>
    /// This extractor generates two extract parameters that must be included in your query.<br />
    /// 1. Batch size. The @BatchSize parameter defaults to 1000.<br/>
    /// 2. Cursor. The name of the cursor parameter to use in your query is the same as the selected property. E.g. for selector x => x.Id, query parameter is @Id
    /// </para>
    /// <para>
    /// How to use with a SQL query:<br />
    /// SELECT * FROM [Table] <br/>WHERE [Id] > @Id <br/>ORDER BY [Id] <br/>LIMIT @BatchSize
    /// </para>
    /// </summary>
    public static IExtractBuilderExtractorStep<TInput, TOutput> WithCursorPagination<TInput, TOutput, TCursor>(
        this IExtractBuilderConnectorStep<TInput, TOutput> connectorStep, 
        Expression<Func<TOutput, TCursor>> cursorProperty, 
        Action<CursorPaginationOptions<TOutput, TCursor>>? configure = null)
    {
        if (!typeof(TCursor).IsValueType)
        {
            throw new ArgumentException($"Cursor must be value type");
        }
        
        var options = new CursorPaginationOptions<TOutput, TCursor>();
        configure?.Invoke(options);

        if (options.Cursor is null)
        {
            options.Cursor = cursorProperty;
        }

        if (options.InitialValue is null)
        {
            options.InitialValue = default(TCursor);
        }

        return connectorStep.WithExtractor<CursorPaginationExtractor<TInput, TOutput, TCursor>>(extractor => extractor.Options = options);
    }
    
    /// <summary>
    /// <para>
    /// Adds the cursor pagination extractor which generates query parameters for batch size and next cursor for each query executed by the connector.
    /// </para>
    /// <para>
    /// This extractor generates two extract parameters that must be included in your query.<br />
    /// 1. Batch size. The @BatchSize parameter defaults to 1000.<br/>
    /// 2. Cursor. The name of the cursor parameter to use in your query is the same as the selected property. E.g. for selector x => x.Id, query parameter is @Id
    /// </para>
    /// <para>
    /// How to use with a SQL query:<br />
    /// SELECT * FROM [Table] <br/>WHERE [Id] > @Id <br/>ORDER BY [Id] <br/>LIMIT @BatchSize
    /// </para>
    /// </summary>
    public static IExtractBuilderExtractorStep<TInput, TOutput> WithCursorPagination<TInput, TOutput>(
        this IExtractBuilderConnectorStep<TInput, TOutput> connectorStep, 
        Expression<Func<TOutput, string>> cursorProperty, 
        Action<CursorPaginationOptions<TOutput, string>>? configure = null)
    {
        var options = new CursorPaginationOptions<TOutput, string>();
        configure?.Invoke(options);

        if (options.Cursor is null)
        {
            options.Cursor = cursorProperty;
        }

        if (options.InitialValue is null)
        {
            options.InitialValue = " ";
        }

        return connectorStep.WithExtractor<CursorPaginationExtractor<TInput, TOutput, string>>(extractor => extractor.Options = options);
    }
}

public class CursorPaginationOptions<TOutput, TCursor>
{
    public Expression<Func<TOutput, TCursor>>? Cursor { get; set; }
    public TCursor? InitialValue { get; set; }
    public CursorOrder OrderBy { get; set; } = CursorOrder.Ascending;
}
