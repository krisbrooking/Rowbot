using Rowbot.Framework.Blocks.Extractors.Pagination;
using System.Linq.Expressions;

namespace Rowbot.Extractors.CursorPagination.CursorTypes
{
    public sealed class ByteCursorType<TSource> : ICursorType<TSource, byte>
    {
        public CursorPaginationExtractorOptions<TSource> ConvertOptions(CursorPaginationOptions<TSource, byte> options)
        {
            if (options.Cursor is null)
            {
                throw new ArgumentException("Cursor must be specified.");
            }

            return new CursorPaginationExtractorOptions<TSource>
            {
                DataPagerFactory = () => GetDataPager(options.Cursor, options.InitialValue, options.OrderBy),
                BatchSize = options.BatchSize,
                ExtractParameterGenerator = options.ExtractParameterGenerator
            };
        }

        public IDataPager<TSource> GetDataPager(Expression<Func<TSource, byte>> cursorExpression, byte initialValue, CursorOrder cursorOrder)
        {
            return new CursorDataPager<TSource, byte>(cursorExpression, initialValue, cursorOrder);
        }
    }

    public sealed class DateTimeCursorType<TSource> : ICursorType<TSource, DateTime>
    {
        public CursorPaginationExtractorOptions<TSource> ConvertOptions(CursorPaginationOptions<TSource, DateTime> options)
        {
            if (options.Cursor is null)
            {
                throw new ArgumentException("Cursor must be specified.");
            }

            return new CursorPaginationExtractorOptions<TSource>
            {
                DataPagerFactory = () => GetDataPager(options.Cursor, options.InitialValue, options.OrderBy),
                BatchSize = options.BatchSize,
                ExtractParameterGenerator = options.ExtractParameterGenerator
            };
        }

        public IDataPager<TSource> GetDataPager(Expression<Func<TSource, DateTime>> cursorExpression, DateTime initialValue, CursorOrder cursorOrder)
        {
            return new CursorDataPager<TSource, DateTime>(cursorExpression, initialValue, cursorOrder);
        }
    }

    public sealed class DateTimeOffsetCursorType<TSource> : ICursorType<TSource, DateTimeOffset>
    {
        public CursorPaginationExtractorOptions<TSource> ConvertOptions(CursorPaginationOptions<TSource, DateTimeOffset> options)
        {
            if (options.Cursor is null)
            {
                throw new ArgumentException("Cursor must be specified.");
            }

            return new CursorPaginationExtractorOptions<TSource>
            {
                DataPagerFactory = () => GetDataPager(options.Cursor, options.InitialValue, options.OrderBy),
                BatchSize = options.BatchSize,
                ExtractParameterGenerator = options.ExtractParameterGenerator
            };
        }

        public IDataPager<TSource> GetDataPager(Expression<Func<TSource, DateTimeOffset>> cursorExpression, DateTimeOffset initialValue, CursorOrder cursorOrder)
        {
            return new CursorDataPager<TSource, DateTimeOffset>(cursorExpression, initialValue, cursorOrder);
        }
    }

    public sealed class DecimalCursorType<TSource> : ICursorType<TSource, decimal>
    {
        public CursorPaginationExtractorOptions<TSource> ConvertOptions(CursorPaginationOptions<TSource, decimal> options)
        {
            if (options.Cursor is null)
            {
                throw new ArgumentException("Cursor must be specified.");
            }

            return new CursorPaginationExtractorOptions<TSource>
            {
                DataPagerFactory = () => GetDataPager(options.Cursor, options.InitialValue, options.OrderBy),
                BatchSize = options.BatchSize,
                ExtractParameterGenerator = options.ExtractParameterGenerator
            };
        }

        public IDataPager<TSource> GetDataPager(Expression<Func<TSource, decimal>> cursorExpression, decimal initialValue, CursorOrder cursorOrder)
        {
            return new CursorDataPager<TSource, decimal>(cursorExpression, initialValue, cursorOrder);
        }
    }

    public sealed class DoubleCursorType<TSource> : ICursorType<TSource, double>
    {
        public CursorPaginationExtractorOptions<TSource> ConvertOptions(CursorPaginationOptions<TSource, double> options)
        {
            if (options.Cursor is null)
            {
                throw new ArgumentException("Cursor must be specified.");
            }

            return new CursorPaginationExtractorOptions<TSource>
            {
                DataPagerFactory = () => GetDataPager(options.Cursor, options.InitialValue, options.OrderBy),
                BatchSize = options.BatchSize,
                ExtractParameterGenerator = options.ExtractParameterGenerator
            };
        }

        public IDataPager<TSource> GetDataPager(Expression<Func<TSource, double>> cursorExpression, double initialValue, CursorOrder cursorOrder)
        {
            return new CursorDataPager<TSource, double>(cursorExpression, initialValue, cursorOrder);
        }
    }

    public sealed class FloatCursorType<TSource> : ICursorType<TSource, float>
    {
        public CursorPaginationExtractorOptions<TSource> ConvertOptions(CursorPaginationOptions<TSource, float> options)
        {
            if (options.Cursor is null)
            {
                throw new ArgumentException("Cursor must be specified.");
            }

            return new CursorPaginationExtractorOptions<TSource>
            {
                DataPagerFactory = () => GetDataPager(options.Cursor, options.InitialValue, options.OrderBy),
                BatchSize = options.BatchSize,
                ExtractParameterGenerator = options.ExtractParameterGenerator
            };
        }

        public IDataPager<TSource> GetDataPager(Expression<Func<TSource, float>> cursorExpression, float initialValue, CursorOrder cursorOrder)
        {
            return new CursorDataPager<TSource, float>(cursorExpression, initialValue, cursorOrder);
        }
    }

    public sealed class GuidCursorType<TSource> : ICursorType<TSource, Guid>
    {
        public CursorPaginationExtractorOptions<TSource> ConvertOptions(CursorPaginationOptions<TSource, Guid> options)
        {
            if (options.Cursor is null)
            {
                throw new ArgumentException("Cursor must be specified.");
            }

            return new CursorPaginationExtractorOptions<TSource>
            {
                DataPagerFactory = () => GetDataPager(options.Cursor, options.InitialValue, options.OrderBy),
                BatchSize = options.BatchSize,
                ExtractParameterGenerator = options.ExtractParameterGenerator
            };
        }

        public IDataPager<TSource> GetDataPager(Expression<Func<TSource, Guid>> cursorExpression, Guid initialValue, CursorOrder cursorOrder)
        {
            return new CursorDataPager<TSource, Guid>(cursorExpression, initialValue, cursorOrder);
        }
    }

    public sealed class IntCursorType<TSource> : ICursorType<TSource, int>
    {
        public CursorPaginationExtractorOptions<TSource> ConvertOptions(CursorPaginationOptions<TSource, int> options)
        {
            if (options.Cursor is null)
            {
                throw new ArgumentException("Cursor must be specified.");
            }

            return new CursorPaginationExtractorOptions<TSource>
            {
                DataPagerFactory = () => GetDataPager(options.Cursor, options.InitialValue, options.OrderBy),
                BatchSize = options.BatchSize,
                ExtractParameterGenerator = options.ExtractParameterGenerator
            };
        }

        public IDataPager<TSource> GetDataPager(Expression<Func<TSource, int>> cursorExpression, int initialValue, CursorOrder cursorOrder)
        {
            return new CursorDataPager<TSource, int>(cursorExpression, initialValue, cursorOrder);
        }
    }

    public sealed class LongCursorType<TSource> : ICursorType<TSource, long>
    {
        public CursorPaginationExtractorOptions<TSource> ConvertOptions(CursorPaginationOptions<TSource, long> options)
        {
            if (options.Cursor is null)
            {
                throw new ArgumentException("Cursor must be specified.");
            }

            return new CursorPaginationExtractorOptions<TSource>
            {
                DataPagerFactory = () => GetDataPager(options.Cursor, options.InitialValue, options.OrderBy),
                BatchSize = options.BatchSize,
                ExtractParameterGenerator = options.ExtractParameterGenerator
            };
        }

        public IDataPager<TSource> GetDataPager(Expression<Func<TSource, long>> cursorExpression, long initialValue, CursorOrder cursorOrder)
        {
            return new CursorDataPager<TSource, long>(cursorExpression, initialValue, cursorOrder);
        }
    }

    public sealed class ShortCursorType<TSource> : ICursorType<TSource, short>
    {
        public CursorPaginationExtractorOptions<TSource> ConvertOptions(CursorPaginationOptions<TSource, short> options)
        {
            if (options.Cursor is null)
            {
                throw new ArgumentException("Cursor must be specified.");
            }

            return new CursorPaginationExtractorOptions<TSource>
            {
                DataPagerFactory = () => GetDataPager(options.Cursor, options.InitialValue, options.OrderBy),
                BatchSize = options.BatchSize,
                ExtractParameterGenerator = options.ExtractParameterGenerator
            };
        }

        public IDataPager<TSource> GetDataPager(Expression<Func<TSource, short>> cursorExpression, short initialValue, CursorOrder cursorOrder)
        {
            return new CursorDataPager<TSource, short>(cursorExpression, initialValue, cursorOrder);
        }
    }

    public sealed class StringCursorType<TSource> : ICursorType<TSource, string>
    {
        public CursorPaginationExtractorOptions<TSource> ConvertOptions(CursorPaginationOptions<TSource, string> options)
        {
            if (options.Cursor is null)
            {
                throw new ArgumentException("Cursor must be specified.");
            }

            return new CursorPaginationExtractorOptions<TSource>
            {
                DataPagerFactory = () => GetDataPager(options.Cursor, options.InitialValue ?? " ", options.OrderBy),
                BatchSize = options.BatchSize,
                ExtractParameterGenerator = options.ExtractParameterGenerator
            };
        }

        public IDataPager<TSource> GetDataPager(Expression<Func<TSource, string>> cursorExpression, string initialValue, CursorOrder cursorOrder)
        {
            return new CursorDataPager<TSource, string>(cursorExpression, initialValue, cursorOrder);
        }
    }
}
