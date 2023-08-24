using Rowbot.Framework.Blocks.Extractors.Pagination;
using Rowbot.Framework.Pipelines.Options;

namespace Rowbot.Extractors.OffsetPagination
{
    public sealed class OffsetPaginationExtractorOptions<TSource> : ExtractorOptions
    {
        public Func<IDataPager<TSource>> DataPagerFactory { get; set; } = () => new DefaultDataPager<TSource>();
    }
}
