namespace Rowbot.Framework.Blocks.Extractors.Pagination
{
    public sealed class OffsetDataPager<T> : IDataPager<T>
    {
        internal readonly int _initialOffset;
        internal bool _isFirstPage = true;
        internal int _resultCount;
        internal OffsetOrder _offsetOrder;

        public OffsetDataPager(int initialOffset, OffsetOrder offsetOrder)
        {
            _initialOffset = initialOffset;
            _offsetOrder = offsetOrder;
            OffsetParameter = "Offset";
        }

        public OffsetDataPager(int initialOffset, OffsetOrder offsetOrder, string offsetParameter) : this(initialOffset, offsetOrder)
        {
            OffsetParameter = offsetParameter;
        }

        public string OffsetParameter { get; set; }
        public bool IsEndOfQuery { get; set; }
        public int TotalResults { get; private set; }

        public ExtractParameterCollection Next()
        {
            var parameters = new ExtractParameterCollection();

            if (_isFirstPage)
            {
                _isFirstPage = false;
                parameters.Add(new ExtractParameter(OffsetParameter, typeof(int), _initialOffset));
                return parameters;
            }

            TotalResults += _resultCount;

            IsEndOfQuery = _resultCount == 0;
            if (!IsEndOfQuery)
            {
                var offsetValue = _offsetOrder == OffsetOrder.Ascending
                    ? _initialOffset + TotalResults
                    : _initialOffset - TotalResults;
                parameters.Add(new ExtractParameter(OffsetParameter, typeof(int), offsetValue));
            }

            _resultCount = 0;

            return parameters;
        }

        public void AddResults(params T[] results)
        {
            _resultCount += results.Length;
        }
    }

    public enum OffsetOrder
    {
        Ascending = 0,
        Descending = 1
    }
}
