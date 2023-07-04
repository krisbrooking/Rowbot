using Rowbot.Loaders.SlowlyChangingDimensions;

namespace Rowbot
{
    public static class SlowlyChangingDimensionLoaderExtensions
    {
        /// <summary>
        /// <para>
        /// Inserts and updates rows using a write connector and includes support for change history.
        /// </para>
        /// <para>
        /// Slowly changing dimension loader updates data in one of two ways.<br/>
        /// Type 1 updates are made in place using the row that already exists.<br/>
        /// Type 2 updates are made by changing the status of the current row to inactive and creating a new row with any changes.<br/>
        /// </para>
        /// <para>
        /// The <see cref="Row.KeyHash"/> property is used to determine whether a row already exists.<br/>
        /// The <see cref="Row.ChangeHash"/> property is used to determine whether any value of a row has changed.
        /// </para>
        /// </summary>
        public static Pipeline WithSlowlyChangingDimension<TTarget>(this IPipelineLoader<TTarget> pipelineLoader)
            where TTarget : Dimension
        {
            return WithSlowlyChangingDimension(pipelineLoader, (options) => new SlowlyChangingDimensionLoaderOptions<TTarget>());
        }

        /// <inheritdoc cref="WithSlowlyChangingDimension{TTarget}(IPipelineLoader{TTarget})"/>
        public static Pipeline WithSlowlyChangingDimension<TTarget>(this IPipelineLoader<TTarget> pipelineLoader, Action<SlowlyChangingDimensionLoaderOptions<TTarget>> configure)
            where TTarget : Dimension
        {
            var options = new SlowlyChangingDimensionLoaderOptions<TTarget>();
            configure?.Invoke(options);

            return pipelineLoader.WithLoader<SlowlyChangingDimensionLoader<TTarget>, SlowlyChangingDimensionLoaderOptions<TTarget>>(options);
        }
    }
}
