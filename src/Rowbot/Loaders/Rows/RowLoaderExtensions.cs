using Rowbot.Loaders.Rows;

namespace Rowbot
{
    public static class RowLoaderExtensions
    {
        /// <summary>
        /// Inserts all rows using a write connector. Updating is not supported.
        /// </summary>
        public static Pipeline CopyRows<TTarget>(this IPipelineLoader<TTarget> pipelineLoader)
        {
            return pipelineLoader.WithLoader<RowLoader<TTarget>, RowLoaderOptions<TTarget>>(new RowLoaderOptions<TTarget>());
        }
    }
}
