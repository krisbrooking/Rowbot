using Rowbot.Entities;

namespace Rowbot.Null;

public class NullWriteConnector<TInput> : IWriteConnector<TInput>
{
    public Task<IEnumerable<TInput>> FindAsync(IEnumerable<TInput> findEntities, Action<IFieldSelector<TInput>> compareFieldsSelector, Action<IFieldSelector<TInput>> resultFieldsSelector)
    {
        return Task.FromResult<IEnumerable<TInput>>([]);
    }

    public Task<int> InsertAsync(IEnumerable<TInput> data)
    {
        return Task.FromResult(0);
    }

    public Task<int> UpdateAsync(IEnumerable<RowUpdate<TInput>> data)
    {
        return Task.FromResult(0);
    }
}