using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace Rowbot.Connectors.Http;

public sealed class JsonEndpointReadConnector<TInput, TOutput>(
    ILogger<JsonEndpointReadConnector<TInput, TOutput>> logger,
    IHttpClientFactory httpClientFactory)
    : IReadConnector<TInput, TOutput>
{
    private readonly ILogger<JsonEndpointReadConnector<TInput, TOutput>> _logger = logger;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public string RequestUri { get; set; } = string.Empty;
    public Action<HttpClient, Uri>? HttpClientConfigurator { get; set; }

    public async Task<IEnumerable<TOutput>> QueryAsync(ExtractParameter[] parameters)
    {
        var result = new List<TOutput>();

        var requestUri = ApplyParametersToRequestUri(RequestUri, parameters);
        
        var httpClient = _httpClientFactory.CreateClient();
        HttpClientConfigurator?.Invoke(httpClient, new Uri(requestUri));

        _logger.LogInformation("HTTP GET {requestUri}", requestUri);
        var response = await httpClient.GetAsync(requestUri);
            
        response.EnsureSuccessStatusCode();
            
        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonNode = JsonSerializer.Deserialize<JsonNode>(responseContent);
        if (jsonNode is JsonArray)
        {
            result = JsonSerializer.Deserialize<List<TOutput>>(responseContent) ?? [];
        }
        else if (jsonNode is JsonObject)
        {
            result = [ JsonSerializer.Deserialize<TOutput>(responseContent)! ];
        }
        
        _logger.LogInformation("Returned {results} results", result.Count());

        return result;
    }

    private string ApplyParametersToRequestUri(string requestUri, IEnumerable<ExtractParameter> parameters)
    {
        foreach (var parameter in parameters)
        {
            requestUri = requestUri.Replace($"@{parameter.ParameterName}", parameter.ParameterValue?.ToString());
        }

        return requestUri;
    }
}