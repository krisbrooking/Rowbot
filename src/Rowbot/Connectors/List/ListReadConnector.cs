namespace Rowbot.Connectors.List;

public sealed class ListReadConnector<TInput, TOutput> : IReadConnector<TInput, TOutput>
{
    public ListReadConnectorOptions<TOutput> Options { get; set; } = new();

    public Task<IEnumerable<TOutput>> QueryAsync(ExtractParameter[] parameters)
    {
        return Task.FromResult(Options.Data.AsEnumerable());
    }
}