using Rowbot.Extractors.OffsetPagination;
using Rowbot.Framework.Blocks.Extractors.Pagination;
using Rowbot.Framework.Pipelines.Options;

namespace Rowbot
{
    public static class OffsetPaginationExtractorExtensions
    {
        /// <summary>
        /// <para>
        /// Adds the offset pagination extractor which generates query parameters for batch size and next offset for each query executed by the connector.
        /// </para>
        /// <para>
        /// This extractor generates two extract parameters that must be included in your query.<br />
        /// 1. Batch size. The @BatchSize parameter defaults to 1000. This can be modified by changing the BatchSize property of the extractor using the <see cref="PipelineBuilderExtensions.WithOptions{TSource}(IPipelineExtractor{TSource}, Action{Framework.Pipelines.Options.ExtractorOptions})"/> extension.<br/>
        /// 2. Offset. The @Offset parameter is set to the start of the next page.
        /// </para>
        /// <para>
        /// How to use with a SQL query:<br />
        /// SELECT * FROM [Table] <br/>ORDER BY [Id]<br/>OFFSET @Offset <br/>LIMIT @BatchSize
        /// </para>
        /// </summary>
        public static IPipelineTransformer<TSource, TSource> WithOffsetPagination<TSource>(this IPipelineExtractor<TSource> pipelineExtractor, Action<OffsetPaginationOptions>? configure = default)
        {
            var localOptions = new OffsetPaginationOptions();
            configure?.Invoke(localOptions);

            var options = new OffsetPaginationExtractorOptions<TSource>
            {
                DataPagerFactory = () => new OffsetDataPager<TSource>(localOptions.InitialValue, localOptions.OrderBy),
                BatchSize = localOptions.BatchSize,
                ExtractParameterGenerator = localOptions.ExtractParameterGenerator
            };

            return pipelineExtractor.WithExtractor<OffsetPaginationExtractor<TSource>, OffsetPaginationExtractorOptions<TSource>>(options);
        }
    }

    public class OffsetPaginationOptions : ExtractorOptions
    {
        public int InitialValue { get; set; } = 0;
        public OffsetOrder OrderBy { get; set; } = OffsetOrder.Ascending;
    }
}
