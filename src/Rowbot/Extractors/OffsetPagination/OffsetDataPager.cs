namespace Rowbot.Extractors.OffsetPagination;

public sealed class OffsetDataPager<TEntity>
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

    public ExtractParameter[] Next()
    {
        var parameters = new List<ExtractParameter>();

        if (_isFirstPage)
        {
            _isFirstPage = false;
            parameters.Add(new ExtractParameter(OffsetParameter, typeof(int), _initialOffset));
            return parameters.ToArray();
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

        return parameters.ToArray();
    }

    public void AddResults(params TEntity[] results)
    {
        _resultCount += results.Length;
    }
}

public enum OffsetOrder
{
    Ascending = 0,
    Descending = 1
}