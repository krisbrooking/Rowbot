using Rowbot.Extractors.CursorPagination;
using Rowbot.Extractors.CursorPagination.CursorTypes;
using Rowbot.Framework.Blocks.Extractors.Pagination;
using Rowbot.Framework.Pipelines.Options;
using System.Linq.Expressions;

namespace Rowbot
{
    public static class CursorPaginationExtractorExtensions
    {
        /// <summary>
        /// <para>
        /// Adds the cursor pagination extractor which generates query parameters for batch size and next cursor for each query executed by the connector.
        /// </para>
        /// <para>
        /// This extractor generates two extract parameters that must be included in your query.<br />
        /// 1. Batch size. The @BatchSize parameter defaults to 1000. This can be modified by changing the BatchSize property of the extractor using the <see cref="PipelineBuilderExtensions.WithOptions{TSource}(IPipelineExtractor{TSource}, Action{Framework.Pipelines.Options.ExtractorOptions})"/> extension.<br/>
        /// 2. Cursor. The name of the cursor parameter to use in your query is the same as the selected property. E.g. for selector x => x.Id, query parameter is @Id
        /// </para>
        /// <para>
        /// How to use with a SQL query:<br />
        /// SELECT * FROM [Table] <br/>WHERE [Id] > @Id <br/>ORDER BY [Id] <br/>LIMIT @BatchSize
        /// </para>
        /// </summary>
        public static IPipelineTransformer<TSource, TSource> WithCursorPagination<TSource>(this IPipelineExtractor<TSource> pipelineExtractor, Expression<Func<TSource, byte>> cursorProperty, Action<CursorPaginationOptions<TSource, byte>>? configure = default)
        {
            var localOptions = new CursorPaginationOptions<TSource, byte>();
            configure?.Invoke(localOptions);
            localOptions.Cursor = cursorProperty;

            var pagingExtractor = new ByteCursorType<TSource>();
            var options = pagingExtractor.ConvertOptions(localOptions);

            return pipelineExtractor.WithExtractor<CursorPaginationExtractor<TSource>, CursorPaginationExtractorOptions<TSource>>(options);
        }

        /// <inheritdoc cref="WithCursorPagination{TSource}(IPipelineExtractor{TSource}, Expression{Func{TSource, byte}}, Action{CursorPaginationOptions{TSource, byte}}?)" />
        public static IPipelineTransformer<TSource, TSource> WithCursorPagination<TSource>(this IPipelineExtractor<TSource> pipelineExtractor, Expression<Func<TSource, DateTime>> cursorProperty, Action<CursorPaginationOptions<TSource, DateTime>>? configure = default)
        {
            var localOptions = new CursorPaginationOptions<TSource, DateTime>();
            configure?.Invoke(localOptions);
            localOptions.Cursor = cursorProperty;

            var pagingExtractor = new DateTimeCursorType<TSource>();
            var options = pagingExtractor.ConvertOptions(localOptions);

            return pipelineExtractor.WithExtractor<CursorPaginationExtractor<TSource>, CursorPaginationExtractorOptions<TSource>>(options);
        }

        /// <inheritdoc cref="WithCursorPagination{TSource}(IPipelineExtractor{TSource}, Expression{Func{TSource, byte}}, Action{CursorPaginationOptions{TSource, byte}}?)" />
        public static IPipelineTransformer<TSource, TSource> WithCursorPagination<TSource>(this IPipelineExtractor<TSource> pipelineExtractor, Expression<Func<TSource, DateTimeOffset>> cursorProperty, Action<CursorPaginationOptions<TSource, DateTimeOffset>>? configure = default)
        {
            var localOptions = new CursorPaginationOptions<TSource, DateTimeOffset>();
            configure?.Invoke(localOptions);
            localOptions.Cursor = cursorProperty;

            var pagingExtractor = new DateTimeOffsetCursorType<TSource>();
            var options = pagingExtractor.ConvertOptions(localOptions);

            return pipelineExtractor.WithExtractor<CursorPaginationExtractor<TSource>, CursorPaginationExtractorOptions<TSource>>(options);
        }

        /// <inheritdoc cref="WithCursorPagination{TSource}(IPipelineExtractor{TSource}, Expression{Func{TSource, byte}}, Action{CursorPaginationOptions{TSource, byte}}?)" />
        public static IPipelineTransformer<TSource, TSource> WithCursorPagination<TSource>(this IPipelineExtractor<TSource> pipelineExtractor, Expression<Func<TSource, decimal>> cursorProperty, Action<CursorPaginationOptions<TSource, decimal>>? configure = default)
        {
            var localOptions = new CursorPaginationOptions<TSource, decimal>();
            configure?.Invoke(localOptions);
            localOptions.Cursor = cursorProperty;

            var pagingExtractor = new DecimalCursorType<TSource>();
            var options = pagingExtractor.ConvertOptions(localOptions);

            return pipelineExtractor.WithExtractor<CursorPaginationExtractor<TSource>, CursorPaginationExtractorOptions<TSource>>(options);
        }

        /// <inheritdoc cref="WithCursorPagination{TSource}(IPipelineExtractor{TSource}, Expression{Func{TSource, byte}}, Action{CursorPaginationOptions{TSource, byte}}?)" />
        public static IPipelineTransformer<TSource, TSource> WithCursorPagination<TSource>(this IPipelineExtractor<TSource> pipelineExtractor, Expression<Func<TSource, double>> cursorProperty, Action<CursorPaginationOptions<TSource, double>>? configure = default)
        {
            var localOptions = new CursorPaginationOptions<TSource, double>();
            configure?.Invoke(localOptions);
            localOptions.Cursor = cursorProperty;

            var pagingExtractor = new DoubleCursorType<TSource>();
            var options = pagingExtractor.ConvertOptions(localOptions);

            return pipelineExtractor.WithExtractor<CursorPaginationExtractor<TSource>, CursorPaginationExtractorOptions<TSource>>(options);
        }

        /// <inheritdoc cref="WithCursorPagination{TSource}(IPipelineExtractor{TSource}, Expression{Func{TSource, byte}}, Action{CursorPaginationOptions{TSource, byte}}?)" />
        public static IPipelineTransformer<TSource, TSource> WithCursorPagination<TSource>(this IPipelineExtractor<TSource> pipelineExtractor, Expression<Func<TSource, float>> cursorProperty, Action<CursorPaginationOptions<TSource, float>>? configure = default)
        {
            var localOptions = new CursorPaginationOptions<TSource, float>();
            configure?.Invoke(localOptions);
            localOptions.Cursor = cursorProperty;

            var pagingExtractor = new FloatCursorType<TSource>();
            var options = pagingExtractor.ConvertOptions(localOptions);

            return pipelineExtractor.WithExtractor<CursorPaginationExtractor<TSource>, CursorPaginationExtractorOptions<TSource>>(options);
        }

        /// <inheritdoc cref="WithCursorPagination{TSource}(IPipelineExtractor{TSource}, Expression{Func{TSource, byte}}, Action{CursorPaginationOptions{TSource, byte}}?)" />
        public static IPipelineTransformer<TSource, TSource> WithCursorPagination<TSource>(this IPipelineExtractor<TSource> pipelineExtractor, Expression<Func<TSource, Guid>> cursorProperty, Action<CursorPaginationOptions<TSource, Guid>>? configure = default)
        {
            var localOptions = new CursorPaginationOptions<TSource, Guid>();
            configure?.Invoke(localOptions);
            localOptions.Cursor = cursorProperty;

            var pagingExtractor = new GuidCursorType<TSource>();
            var options = pagingExtractor.ConvertOptions(localOptions);

            return pipelineExtractor.WithExtractor<CursorPaginationExtractor<TSource>, CursorPaginationExtractorOptions<TSource>>(options);
        }

        /// <inheritdoc cref="WithCursorPagination{TSource}(IPipelineExtractor{TSource}, Expression{Func{TSource, byte}}, Action{CursorPaginationOptions{TSource, byte}}?)" />
        public static IPipelineTransformer<TSource, TSource> WithCursorPagination<TSource>(this IPipelineExtractor<TSource> pipelineExtractor, Expression<Func<TSource, int>> cursorProperty, Action<CursorPaginationOptions<TSource, int>>? configure = default)
        {
            var localOptions = new CursorPaginationOptions<TSource, int>();
            configure?.Invoke(localOptions);
            localOptions.Cursor = cursorProperty;

            var pagingExtractor = new IntCursorType<TSource>();
            var options = pagingExtractor.ConvertOptions(localOptions);

            return pipelineExtractor.WithExtractor<CursorPaginationExtractor<TSource>, CursorPaginationExtractorOptions<TSource>>(options);
        }

        /// <inheritdoc cref="WithCursorPagination{TSource}(IPipelineExtractor{TSource}, Expression{Func{TSource, byte}}, Action{CursorPaginationOptions{TSource, byte}}?)" />
        public static IPipelineTransformer<TSource, TSource> WithCursorPagination<TSource>(this IPipelineExtractor<TSource> pipelineExtractor, Expression<Func<TSource, long>> cursorProperty, Action<CursorPaginationOptions<TSource, long>>? configure = default)
        {
            var localOptions = new CursorPaginationOptions<TSource, long>();
            configure?.Invoke(localOptions);
            localOptions.Cursor = cursorProperty;

            var pagingExtractor = new LongCursorType<TSource>();
            var options = pagingExtractor.ConvertOptions(localOptions);

            return pipelineExtractor.WithExtractor<CursorPaginationExtractor<TSource>, CursorPaginationExtractorOptions<TSource>>(options);
        }

        /// <inheritdoc cref="WithCursorPagination{TSource}(IPipelineExtractor{TSource}, Expression{Func{TSource, byte}}, Action{CursorPaginationOptions{TSource, byte}}?)" />
        public static IPipelineTransformer<TSource, TSource> WithCursorPagination<TSource>(this IPipelineExtractor<TSource> pipelineExtractor, Expression<Func<TSource, short>> cursorProperty, Action<CursorPaginationOptions<TSource, short>>? configure = default)
        {
            var localOptions = new CursorPaginationOptions<TSource, short>();
            configure?.Invoke(localOptions);
            localOptions.Cursor = cursorProperty;

            var pagingExtractor = new ShortCursorType<TSource>();
            var options = pagingExtractor.ConvertOptions(localOptions);

            return pipelineExtractor.WithExtractor<CursorPaginationExtractor<TSource>, CursorPaginationExtractorOptions<TSource>>(options);
        }

        /// <inheritdoc cref="WithCursorPagination{TSource}(IPipelineExtractor{TSource}, Expression{Func{TSource, byte}}, Action{CursorPaginationOptions{TSource, byte}}?)" />
        public static IPipelineTransformer<TSource, TSource> WithCursorPagination<TSource>(this IPipelineExtractor<TSource> pipelineExtractor, Expression<Func<TSource, string>> cursorProperty, Action<CursorPaginationOptions<TSource, string>>? configure = default)
        {
            var localOptions = new CursorPaginationOptions<TSource, string>();
            configure?.Invoke(localOptions);
            localOptions.Cursor = cursorProperty;

            var pagingExtractor = new StringCursorType<TSource>();
            var options = pagingExtractor.ConvertOptions(localOptions);

            return pipelineExtractor.WithExtractor<CursorPaginationExtractor<TSource>, CursorPaginationExtractorOptions<TSource>>(options);
        }
    }

    public class CursorPaginationOptions<TSource, TCursor> : ExtractorOptions
    {
        internal Expression<Func<TSource, TCursor>>? Cursor { get; set; }
        public TCursor? InitialValue { get; set; }
        public CursorOrder OrderBy { get; set; } = CursorOrder.Ascending;
    }
}
