using Rowbot.Framework.Blocks.Extractors.Pagination;
using System.Linq.Expressions;

namespace Rowbot.Extractors.CursorPagination.CursorTypes
{
    public interface ICursorType<TSource, TCursor>
    {
        CursorPaginationExtractorOptions<TSource> ConvertOptions(CursorPaginationOptions<TSource, TCursor> configure);
        IDataPager<TSource> GetDataPager(Expression<Func<TSource, TCursor>> cursorExpression, TCursor initialValue, CursorOrder cursorOrder);
    }
}
