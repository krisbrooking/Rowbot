using Microsoft.Extensions.Logging;

namespace Rowbot.Connectors.Json;

public class JsonReadConnector<TInput, TOutput>(
    ILogger<JsonReadConnector<TInput, TOutput>> logger)
    : IReadConnector<TInput, TOutput>
{
    private readonly ILogger<JsonReadConnector<TInput, TOutput>> _logger = logger;
        
    public JsonReadConnectorOptions<TOutput> Options { get; set; } = new();

    public Task<IEnumerable<TOutput>> QueryAsync(ExtractParameter[] parameters)
    {
        var result = new List<TOutput>();
        var filter = Options.FilterExpression?.Compile();
        if (filter != null)
        {
            _logger.LogInformation("Query with filter {filter}", Options.FilterDescription);
        }

        using (var stream = new FileStream(Options.FilePath, FileMode.Open))
        using (var reader = new JsonStreamReader(stream))
        {
            foreach (var record in reader.GetRecords<TOutput>())
            {
                if (filter is null || filter(record))
                {
                    result.Add(record);
                }
            }
        }

        return Task.FromResult(result.AsEnumerable());
    }
}