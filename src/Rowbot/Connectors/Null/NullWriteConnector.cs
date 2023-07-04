using Rowbot.Entities;

namespace Rowbot.Connectors.Null
{
    /// <summary>
    /// NullWriteConnector can be used as a placeholder for <see cref="ILoader{TSource, TOptions}.Connector"/> in the constructor of a custom loader.
    /// </summary>
    public sealed class NullWriteConnector<TTarget> : IWriteConnector<TTarget, NullConnectorOptions>
    {
        public NullWriteConnector()
        {
            Options = new();
        }

        public NullConnectorOptions Options { get; set; }

        public Task<IEnumerable<TTarget>> FindAsync(
            IEnumerable<TTarget> findEntities, 
            Action<IFieldSelector<TTarget>> compareFieldsSelector, 
            Action<IFieldSelector<TTarget>> resultFieldsSelector)
            => Task.FromResult(Enumerable.Empty<TTarget>());

        public Task<int> InsertAsync(IEnumerable<TTarget> data) => Task.FromResult(0);

        public Task<int> UpdateAsync(IEnumerable<Update<TTarget>> data) => Task.FromResult(0);
    }
}
