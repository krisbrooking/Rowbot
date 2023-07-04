﻿using System.Linq.Expressions;

namespace Rowbot.Connectors.Json
{
    public sealed class JsonReadConnectorOptions<TSource>
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
        public Expression<Func<TSource, bool>>? FilterExpression { get; set; }
        internal string? FilterDescription => FilterExpression?.ToString();
    }
}
