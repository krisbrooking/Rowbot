using System.Text.Json;

namespace Rowbot.Connectors.Json;

internal sealed class JsonBufferContext
{
    public JsonBufferContext()
    {
        ReaderState = new JsonReaderState();
    }

    /// <summary>
    /// Resets state for the next buffer. Also cleans up partial parsing that can occur when
    /// the last opened object does not fit within the remaining buffer.
    /// </summary>
    public void ResetState(JsonReaderState readerState)
    {
        ReaderState = readerState;
        BufferBytesRead = 0;
        IsCompleteObjectInBuffer = false;

        // Reset partial parsing
        IsCurrentPositionInsideStringValue = false;
        OpenArrayDepth = 1;
        OpenObjectDepth = 0;
    }

    public bool IsEndOfStream { get; set; }
    public bool IsSingleObjectStream { get; set; }
    public int BufferBytesRead { get; set; }
    public int OpenArrayDepth { get; set; }
    public int OpenObjectDepth { get; set; }
    public bool IsCurrentPositionInsideStringValue { get; set; }
    public bool IsCompleteObjectInBuffer { get; set; }
    public int EndArrayIndex { get; set; }
    public int EndObjectIndex { get; set; }
    public JsonReaderState ReaderState { get; set; }
}