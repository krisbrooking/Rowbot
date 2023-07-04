namespace Rowbot.Framework.Blocks.Extractors.Pagination
{
    public interface IDataPager<T>
    {
        /// <summary>
        /// Indicates whether paging should continue
        /// </summary>
        public bool IsEndOfQuery { get; set; }

        /// <summary>
        /// Generates query parameters for the next query.
        /// </summary>
        public ExtractParameterCollection Next();

        /// <summary>
        /// Caches results from the current query. These can then be used to generate the cursor
        /// for the next query.
        /// </summary>
        public void AddResults(params T[] results);

        public int TotalResults { get; }
    }
}
