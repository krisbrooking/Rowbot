using Rowbot.Entities;

namespace Rowbot.Connectors.List;

public sealed class ListWriteConnector<TInput> : IWriteConnector<TInput>
{
    public ListWriteConnectorOptions<TInput> Options { get; set; } = new();
    
    public Task<IEnumerable<TInput>> FindAsync(
        IEnumerable<TInput> findEntities, 
        Action<IFieldSelector<TInput>> compareFieldsSelector, 
        Action<IFieldSelector<TInput>> resultFieldsSelector)
    {
        throw new NotImplementedException();
    }

    public Task<int> InsertAsync(IEnumerable<TInput> data)
    {
        var count = 0;
        foreach (var item in data)
        {
            Options.Target.Add(item);
            count++;
        }
        
        return Task.FromResult(count);
    }

    public Task<int> UpdateAsync(IEnumerable<RowUpdate<TInput>> data)
    {
        throw new NotImplementedException();
    }
}