using System.Text.Json;

namespace Rowbot.Connectors.Json
{
    internal class JsonStreamReader : IDisposable
    {
        private Stream _stream;
        private bool _disposed;

        private const byte OPEN_ARRAY = 91;
        private const byte CLOSE_ARRAY = 93;
        private const byte OPEN_OBJECT = 123;
        private const byte CLOSE_OBJECT = 125;
        private const byte ESCAPE = 92;
        private const byte DOUBLE_QUOTE = 34;

        /// <summary>
        /// Creates a new Json reader using the given <see cref="Stream"/>
        /// </summary>
        /// <param name="stream">The stream</param>
        public JsonStreamReader(Stream stream)
        {
            _stream = stream;
        }

        /// <summary>
        /// Gets records converted into <see cref="Type"/> <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">Type of the deserialized JSON object</typeparam>
        /// <param name="bufferLength">Initial read buffer size (number of bytes). Buffer will increase in size when necessary.</param>
        /// <returns>Stream of records of <see cref="Type"/> <typeparamref name="T"/></returns>
        public IEnumerable<T> GetRecords<T>(int bufferLength = 4096)
        {
            JsonBufferContext context = new();
            var buffer = new byte[bufferLength];

            while (!context.IsEndOfStream)
            {
                _stream.Read(buffer.AsSpan(0, buffer.Length));
                context = InspectBuffer(context, buffer, context.BufferBytesRead);

                if (!context.IsCompleteObjectInBuffer)
                {
                    Array.Resize(ref buffer, buffer.Length * 3 / 2);
                    _stream.Position = Math.Max(_stream.Position - buffer.Length, 0);
                    continue;
                }

                if (context.IsEndOfStream)
                {
                    // Zero bytes after root array closed
                    for (var i = context.EndArrayIndex + 1; i < buffer.Length; i++)
                    {
                        buffer[i] = 0;
                    }
                }

                var deserializeResult = DeserializeBuffer<T>(context, buffer);

                foreach (var item in deserializeResult.Items)
                {
                    yield return item;
                }                

                if (!context.IsEndOfStream)
                {
                    if (context.IsSingleObjectStream)
                    {
                        break;
                    }

                    _stream.Position = _stream.Position - (buffer.Length - context.EndObjectIndex) + 1;
                    context.ResetState(deserializeResult.ReaderState);
                }
            }
        }

        /// <summary>
        /// Parses the current buffer one byte at a time to determine the start and end of the root level array and 
        /// of root level objects. Updates the <see cref="JsonBufferContext"/> with metadata about data in the buffer.
        /// </summary>
        internal JsonBufferContext InspectBuffer(JsonBufferContext context, byte[] buffer, int startIndex)
        {
            byte current = 0;
            byte previous = 0;
            int bufferBytesRead = 0;

            for (var i = startIndex; i < buffer.Length; i++)
            {
                bufferBytesRead++;
                current = buffer[i];
                previous = i > 0 ? buffer[i - 1] : (byte)0;

                if (current == DOUBLE_QUOTE && 
                    previous != ESCAPE)
                {
                    context.IsCurrentPositionInsideStringValue = !context.IsCurrentPositionInsideStringValue;
                }

                if (context.IsCurrentPositionInsideStringValue)
                {
                    continue;
                }

                if (current == OPEN_ARRAY)
                {
                    context.OpenArrayDepth++;
                }
                else if (current == OPEN_OBJECT)
                {
                    context.OpenObjectDepth++;
                    if (context.OpenArrayDepth == 0)
                    {
                        context.IsSingleObjectStream = true;
                    }
                }
                else if (current == CLOSE_ARRAY)
                {
                    context.OpenArrayDepth--;
                    if (context.OpenArrayDepth == 0)
                    {
                        context.EndArrayIndex = i;
                        context.IsEndOfStream = true;
                        break;
                    }
                }
                else if (current == CLOSE_OBJECT)
                {
                    context.OpenObjectDepth--;
                    if (context.OpenObjectDepth == 0)
                    {
                        context.EndObjectIndex = i;
                        context.IsCompleteObjectInBuffer = true;
                    }
                }
            }

            context.BufferBytesRead += bufferBytesRead;

            return context;
        }

        /// <summary>
        /// Deserializes one or more objects. The buffer is guaranteed to contain at least one complete object.
        /// </summary>
        private (JsonReaderState ReaderState, IEnumerable<T> Items) DeserializeBuffer<T>(JsonBufferContext context, byte[] buffer)
        {
            var result = new List<T>();

            var reader = new Utf8JsonReader(buffer.AsSpan(0, Math.Max(context.EndObjectIndex, context.EndArrayIndex) + 1), context.IsEndOfStream, context.ReaderState);

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    var record = JsonSerializer.Deserialize<T>(ref reader);
                    if (record is not null)
                    {
                        result.Add(record);
                    }
                }
            }

            return (reader.CurrentState, result);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _stream?.Dispose();
            }

            _disposed = true;
        }
    }
}
