using System.Linq.Expressions;

namespace Rowbot.Connectors.Json;

public static class JsonConnectorExtensions
{
    /// <summary>
    /// Extracts data from a JSON file.
    /// </summary>
    public static IExtractBuilderConnectorStep<TInput, TOutput> ExtractJson<TInput, TOutput>(
        this IExtractBuilder<TInput, TOutput> builder, 
        string filePath, 
        Action<JsonReadConnectorOptions<TOutput>>? configure = null)
    {
        var options = new JsonReadConnectorOptions<TOutput>();
        options.SetFilePath(filePath);
        configure?.Invoke(options);

        return builder.WithConnector<JsonReadConnector<TInput, TOutput>>(connector => connector.Options = options);
    }
}
    
public sealed class JsonReadConnectorOptions<TOutput>
{
    internal void SetFilePath(string filePath)
    {
        var fileExtension = Path.GetExtension(filePath);
        if (string.IsNullOrEmpty(fileExtension))
        {
            throw new ArgumentException($"{nameof(filePath)} does not include file extension");
        }

        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var directoryPath = string.IsNullOrEmpty(Path.GetDirectoryName(filePath)) ? "." : Path.GetDirectoryName(filePath);

        FilePath = $"{directoryPath}\\{fileName}{fileExtension}".Trim('\\');
    }

    internal string FilePath { get; set; } = string.Empty;
    public Expression<Func<TOutput, bool>>? FilterExpression { get; set; }
    internal string? FilterDescription => FilterExpression?.ToString();
}