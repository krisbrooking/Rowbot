using Rowbot.Framework.Blocks.Extractors.Pagination;

namespace Rowbot.Extractors.OffsetPagination
{
    public sealed class OffsetPaginationExtractorOptions<TSource>
    {
        public Func<IDataPager<TSource>> DataPagerFactory { get; set; } = () => new DefaultDataPager<TSource>();
    }
}
