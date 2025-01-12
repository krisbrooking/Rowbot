using Rowbot.Connectors.Json;
using System.Text;

namespace Rowbot.UnitTests.Connectors.Builtin.Json
{
    public class JsonBufferContextTests
    {
        [Fact]
        public void InspectBuffer_Should_IncrementOpenArraysAndOpenObjects()
        {
            var buffer = Encoding.UTF8.GetBytes("[{\"First_Name\": \"Alice\"");

            var reader = new JsonStreamReader(new MemoryStream());
            var context = reader.InspectBuffer(new JsonBufferContext(), buffer, 0);

            Assert.Equal(1, context.OpenArrayDepth);
            Assert.Equal(1, context.OpenObjectDepth);
        }

        [Fact]
        public void InspectBuffer_Should_SetIsEndOfStreamFlag()
        {
            var buffer = Encoding.UTF8.GetBytes("[{\"First_Name\": \"Alice\"}]");

            var reader = new JsonStreamReader(new MemoryStream());
            var context = reader.InspectBuffer(new JsonBufferContext(), buffer, 0);

            Assert.True(context.IsEndOfStream);
            Assert.Equal(24, context.EndArrayIndex);
        }

        [Fact]
        public void InspectBuffer_Should_SetIsCurrentPositionInsideStringValueFlag_WhenStringIsOpenedWithDoubleQuote()
        {
            var buffer = Encoding.UTF8.GetBytes("[{\"First");

            var reader = new JsonStreamReader(new MemoryStream());
            var context = reader.InspectBuffer(new JsonBufferContext(), buffer, 0);

            Assert.True(context.IsCurrentPositionInsideStringValue);
        }

        [Fact]
        public void InspectBuffer_Should_ClearIsCurrentPositionInsideStringValueFlag_WhenStringIsClosedWithDoubleQuote()
        {
            var buffer = Encoding.UTF8.GetBytes("[{\"First_Name\"");

            var reader = new JsonStreamReader(new MemoryStream());
            var context = reader.InspectBuffer(new JsonBufferContext(), buffer, 0);

            Assert.False(context.IsCurrentPositionInsideStringValue);
        }

        [Fact]
        public void InspectBuffer_Should_IgnoreIsCurrentPositionInsideStringValueSet_WhenDoubleQuoteIsEscaped()
        {
            var buffer = Encoding.UTF8.GetBytes("[{\"First_Name\": \"\\\"Alice");

            var reader = new JsonStreamReader(new MemoryStream());
            var context = reader.InspectBuffer(new JsonBufferContext(), buffer, 0);

            Assert.True(context.IsCurrentPositionInsideStringValue);
        }

        [Fact]
        public void InspectBuffer_Should_IgnoreOpenArrayAndOpenObject_WhileStringValueIsOpen()
        {
            var buffer = Encoding.UTF8.GetBytes("[{\"First_Name\": \"[{Alice");

            var reader = new JsonStreamReader(new MemoryStream());
            var context = reader.InspectBuffer(new JsonBufferContext(), buffer, 0);

            Assert.True(context.IsCurrentPositionInsideStringValue);
            Assert.Equal(1, context.OpenArrayDepth);
            Assert.Equal(1, context.OpenObjectDepth);
        }

        [Fact]
        public void InspectBuffer_Should_IncrementOpenArrayAndOpenObject_ForNestedArraysAndObjects()
        {
            var buffer = Encoding.UTF8.GetBytes("[{\"Person\": [{\"Name\"");

            var reader = new JsonStreamReader(new MemoryStream());
            var context = reader.InspectBuffer(new JsonBufferContext(), buffer, 0);

            Assert.Equal(2, context.OpenArrayDepth);
            Assert.Equal(2, context.OpenObjectDepth);
        }
    }
}
