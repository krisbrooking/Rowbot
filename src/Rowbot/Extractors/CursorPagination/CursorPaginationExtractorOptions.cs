using Rowbot.Framework.Blocks.Extractors.Pagination;
using Rowbot.Framework.Pipelines.Options;

namespace Rowbot.Extractors.CursorPagination
{
    public sealed class CursorPaginationExtractorOptions<TSource> : ExtractorOptions
    {
        public Func<IDataPager<TSource>> DataPagerFactory { get; set; } = () => new DefaultDataPager<TSource>();
    }
}
