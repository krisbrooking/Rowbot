namespace Rowbot.Framework.Blocks.Extractors.Pagination
{
    public sealed class DefaultDataPager<T> : IDataPager<T>
    {
        internal bool _isFirstPage = true;

        public bool IsEndOfQuery { get; set; }
        public int TotalResults => 0;

        public ExtractParameterCollection Next()
        {
            if (_isFirstPage)
            {
                _isFirstPage = false;
                return new ExtractParameterCollection();
            }

            IsEndOfQuery = !IsEndOfQuery;

            return new ExtractParameterCollection();
        }

        public void AddResults(params T[] results) { return; }
    }
}
