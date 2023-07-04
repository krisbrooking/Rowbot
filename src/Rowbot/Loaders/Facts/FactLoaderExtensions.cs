using Rowbot.Loaders.Facts;

namespace Rowbot
{
    public static class FactLoaderExtensions
    {
        /// <summary>
        /// <para>
        /// Inserts new rows using a write connector. Updating is not supported.
        /// </para>
        /// <para>
        /// Fact loader does not insert a row that already exists. The <see cref="Row.KeyHash"/> property is used to determine whether a row already exists.</para>
        /// </summary>
        public static Pipeline WithFact<TTarget>(this IPipelineLoader<TTarget> pipelineLoader)
            where TTarget : Fact
        {
            return pipelineLoader.WithLoader<FactLoader<TTarget>, FactLoaderOptions<TTarget>>(new FactLoaderOptions<TTarget>());
        }
    }
}
