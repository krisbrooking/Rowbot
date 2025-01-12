using Rowbot.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace Rowbot.Extractors.CursorPagination;

public sealed class CursorDataPager<TEntity, TCursor>
{
    internal readonly Func<TEntity, TCursor> _cursorAccessor;
    internal readonly Func<IEnumerable<TCursor>, TCursor> _nextCursorDelegate;
    internal readonly TCursor _initialValue;
    internal bool _isFirstPage = true;
    internal HashSet<TCursor> _data;
    internal Type _cursorType;
    internal CursorOrder _cursorOrder;

    /// <summary>
    /// Cursor data pager generates query parameters for page size and next cursor for each query
    /// executed by the connector.
    /// </summary>
    /// <param name="cursorSelector">Cursor selector</param>
    /// <param name="initialValue">Inital value of cursor</param>
    /// <param name="cursorOrder">Cursor order, must match order defined in query</param>
    public CursorDataPager(Expression<Func<TEntity, TCursor>>? cursorSelector, TCursor? initialValue, CursorOrder cursorOrder)
    {
        MemberExpression memberExpression = Ensure.ArgumentIsMemberExpression(cursorSelector);
        PropertyInfo cursorProperty = Ensure.MemberExpressionTargetsProperty(memberExpression);

        _data = new HashSet<TCursor>();
        _initialValue = Ensure.ArgumentIsNotNull(initialValue);
        _cursorOrder = cursorOrder;
        CursorParameter = cursorProperty.Name;
        _cursorType = cursorProperty.PropertyType;

        _cursorAccessor = cursorSelector!.Compile();

        _nextCursorDelegate = BuildNextCursorExpression(cursorProperty).Compile();
    }

    /// <summary>
    /// Cursor data pager generates query parameters for page size and next cursor for each query
    /// executed by the connector.
    /// </summary>
    /// <param name="cursorSelector">Cursor selector</param>
    /// <param name="initialValue">Inital value of cursor</param>
    /// <param name="cursorOrder">Cursor order, must match order defined in query</param>
    /// <param name="cursorParameter">Cursor parameter name. Default = cursor property name</param>
    public CursorDataPager(Expression<Func<TEntity, TCursor>> cursorSelector, TCursor initialValue, CursorOrder cursorOrder, string cursorParameter)
        : this(cursorSelector, initialValue, cursorOrder)
    {
        CursorParameter = cursorParameter;
    }
    public bool IsEndOfQuery { get; set; }
    public int TotalResults { get; private set; }

    /// <summary>
    /// Cursor parameter name. Default = cursor property name
    /// </summary>
    public string CursorParameter { get; set; }

    /// <summary>
    /// Generates page size and cursor query parameters for the next query.
    /// </summary>
    public ExtractParameter[] Next()
    {
        var parameters = new List<ExtractParameter>();

        if (_isFirstPage)
        {
            _isFirstPage = false;
            parameters.Add(new ExtractParameter(CursorParameter, _cursorType, _initialValue!));
            return parameters.ToArray();
        }

        TotalResults += _data.Count;

        IsEndOfQuery = _data.Count == 0;
        if (!IsEndOfQuery)
        {
            var nextCursorValue = _nextCursorDelegate(_data);
            if (nextCursorValue is null)
            {
                throw new InvalidOperationException("NextCursorDelegate did not return a value");
            }

            parameters.Add(new ExtractParameter(CursorParameter, _cursorType, nextCursorValue));
        }

        _data.Clear();

        return parameters.ToArray();
    }

    /// <summary>
    /// Caches results from the current query. These are then used to generate the cursor
    /// for the next query.
    /// </summary>
    public void AddResults(params TEntity[] results)
    {
        foreach (var result in results)
        {
            var cursorValue = _cursorAccessor(result);
            if (cursorValue is not null)
            {
                _data.Add(cursorValue);
            }
        }
    }

    internal Expression<Func<IEnumerable<TCursor>, TCursor>> BuildNextCursorExpression(PropertyInfo cursor)
    {
        var sourceType = typeof(IEnumerable<TCursor>);
        var sourceTypeParameter = Expression.Parameter(sourceType, "source");

        var aggregateFunction = _cursorOrder == CursorOrder.Ascending ? nameof(Enumerable.MaxBy) : nameof(Enumerable.MinBy);
        var aggregateParameter = Expression.Parameter(typeof(TCursor), "m");
        var aggregateLambda = Expression.Lambda<Func<TCursor, TCursor>>(aggregateParameter, aggregateParameter);
        var body = Expression.Call(typeof(Enumerable), aggregateFunction, new[] { typeof(TCursor), typeof(TCursor) }, sourceTypeParameter, aggregateLambda);

        return Expression.Lambda<Func<IEnumerable<TCursor>, TCursor>>(body, sourceTypeParameter);
    }
}

public enum CursorOrder
{
    Ascending = 0,
    Descending = 1
}