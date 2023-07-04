using Rowbot.Connectors.Json;
using Rowbot.UnitTests.Setup;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Rowbot.UnitTests.Connectors.Json
{
    public class JsonStreamReaderTests
    {
        [Fact]
        public void GetRecords_Should_PartiallyDeserialize_WhenEntityMissingProperties()
        {
            var source = "[{\"First_Name\": \"Alice\"}]";

            var deserialized = false;

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(source)))
            using (var reader = new JsonStreamReader(stream))
            {
                var records = reader.GetRecords<SourcePerson>().ToList();

                deserialized = records.Any();
            }

            Assert.True(deserialized);
        }

        [Fact]
        public void GetRecords_Should_Deserialize_WhenEntityIsArray()
        {
            var source = "[{\"Id\": 1, \"First_Name\": \"Alice\"}, {\"Id\": 2, \"First_Name\": \"Bob\"}]";

            var firstPersonName = string.Empty;
            var secondPersonName = string.Empty;

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(source)))
            using (var reader = new JsonStreamReader(stream))
            {
                var records = reader.GetRecords<SourcePerson>().ToList();

                firstPersonName = records.SingleOrDefault(x => x.Id == 1)?.First_Name;
                secondPersonName = records.SingleOrDefault(x => x.Id == 2)?.First_Name;
            }

            Assert.Equal("Alice", firstPersonName);
            Assert.Equal("Bob", secondPersonName);
        }

        [Fact]
        public void GetRecords_Should_ThrowJsonException_WhenEntityHasInvalidType()
        {
            var source = "[{\"First_Name\": 1}]";

            Assert.ThrowsAny<JsonException>(() =>
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(source)))
                using (var reader = new JsonStreamReader(stream))
                {
                    var records = reader.GetRecords<SourcePerson>().ToList();
                }
            });
        }

        [Fact]
        public void GetRecords_Should_HonourIgnoreAttribute_WhenEntityHasJsonAttribute()
        {
            var source = "[{\"Id\": 1, \"Ignored\": \"Value\"}]";

            var ignored = false;

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(source)))
            using (var reader = new JsonStreamReader(stream))
            {
                var records = reader.GetRecords<JsonStreamReaderIgnoredPropertyClass>().ToList();
                ignored = records.First().Ignored is null;
            }

            Assert.True(ignored);
        }

        [Fact]
        public void GetRecords_Should_AddRootArrayAndDeserialize_WhenJsonHasNoRootArray()
        {
            var source = "{\"First_Name\": \"Alice\"}";

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(source)))
            using (var reader = new JsonStreamReader(stream))
            {
                var records = reader.GetRecords<SourcePerson>().ToList();
            }
        }

        [Fact]
        public void GetRecords_Should_ResizeBufferAndDeserialize_WhenBufferLengthIsSmallerThanObjectLength()
        {
            var source = "[{\"Id\": 1, \"First_Name\": \"Alice\"}, {\"Id\": 2, \"First_Name\": \"Bob\"}]";
            var bufferLength = 8;

            var firstPersonName = string.Empty;
            var secondPersonName = string.Empty;

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(source)))
            using (var reader = new JsonStreamReader(stream))
            {
                var records = reader.GetRecords<SourcePerson>(bufferLength).ToList();

                firstPersonName = records.SingleOrDefault(x => x.Id == 1)?.First_Name;
                secondPersonName = records.SingleOrDefault(x => x.Id == 2)?.First_Name;
            }

            Assert.Equal("Alice", firstPersonName);
            Assert.Equal("Bob", secondPersonName);
        }

        [Fact]
        public void GetRecords_Should_Deserialize_NestedArray()
        {
            var source = "[{\"Id\": 1, \"IntArray\": [1,2,3]}]";
            List<JsonStreamReaderNestedClass> records = new();

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(source)))
            using (var reader = new JsonStreamReader(stream))
            {
                records = reader.GetRecords<JsonStreamReaderNestedClass>().ToList();
            }

            Assert.Equal(3, records.First().IntArray!.Last());
        }

        [Fact]
        public void GetRecords_Should_Deserialize_NestedObjectArray()
        {
            var source = "[{\"Id\": 1, \"ObjectArray\": [{ \"Id\": 1 }, { \"Id\": 2 }, { \"Id\": 3 }]}]";
            List<JsonStreamReaderNestedClass> records = new();

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(source)))
            using (var reader = new JsonStreamReader(stream))
            {
                records = reader.GetRecords<JsonStreamReaderNestedClass>().ToList();
            }

            Assert.Equal(3, records.First().ObjectArray!.Last().Id);
        }

        internal class JsonStreamReaderIgnoredPropertyClass
        {
            public int Id { get; set; }
            [JsonIgnore]
            public string? Ignored { get; set; }
        }

        internal class JsonStreamReaderNestedClass
        {
            public int Id { get; set; }
            public int[]? IntArray { get; set; }
            public JsonStreamReaderNestedClass[]? ObjectArray { get; set; }
        }
    }
}
