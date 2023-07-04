using Rowbot.Framework.Blocks.Extractors.Pagination;

namespace Rowbot.Extractors.CursorPagination
{
    public sealed class CursorPaginationExtractorOptions<TSource>
    {
        public Func<IDataPager<TSource>> DataPagerFactory { get; set; } = () => new DefaultDataPager<TSource>();
    }
}
