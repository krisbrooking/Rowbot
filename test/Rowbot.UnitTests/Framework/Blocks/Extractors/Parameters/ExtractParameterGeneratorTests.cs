using Rowbot.Framework.Blocks.Extractors.Parameters;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Rowbot.UnitTests.Framework.Blocks.Extractors.Parameters
{
    public class ExtractParameterGeneratorTests
    {
        [Fact]
        public async Task Add_Should_QueueSingleParameterAsFactory()
        {
            var generator = new ExtractParameterGenerator();

            generator.Add(new ExtractParameter("TestName", typeof(string), "TestValue"));

            var result = await GetParametersAsync(generator);

            Assert.Single(result);
            Assert.Single(result.First());
        }

        [Fact]
        public async Task Add_Should_QueueMultipleSingleParametersAsSeparateFactories()
        {
            var generator = new ExtractParameterGenerator();

            generator.Add(new ExtractParameter("TestName1", typeof(string), "TestValue1"));
            generator.Add(new ExtractParameter("TestName2", typeof(string), "TestValue2"));

            var result = await GetParametersAsync(generator);

            Assert.Equal(2, result.Count);
            Assert.Equal(2, result.SelectMany(x => x).Count());
        }

        [Fact]
        public async Task Add_Should_QueueSingleParameterAndCollectionAsSeparateFactories()
        {
            var generator = new ExtractParameterGenerator();

            generator.Add(new ExtractParameter("TestName1", typeof(string), "TestValue1"));
            generator.Add(new ExtractParameterCollection(new ExtractParameter("TestName2", typeof(string), "TestValue2"), new ExtractParameter("TestName3", typeof(string), "TestValue3")));

            var result = await GetParametersAsync(generator);

            Assert.Equal(2, result.Count);
            Assert.Single(result.First());
            Assert.Equal(2, result.Last().Count());
        }

        [Fact]
        public async Task Add_Should_QueueFactories()
        {
            var generator = new ExtractParameterGenerator();

            generator.Add(() => new List<ExtractParameterCollection> { new ExtractParameterCollection(new ExtractParameter("TestName1", typeof(string), "TestValue1")) });
            generator.Add(async () => await Task.FromResult(new List<ExtractParameterCollection> { new ExtractParameterCollection(new ExtractParameter("TestName2", typeof(string), "TestValue2")) }));

            var result = await GetParametersAsync(generator);

            Assert.Equal(2, result.Count);
            Assert.Equal(2, result.SelectMany(x => x).Count());
        }

        private async Task<List<ExtractParameterCollection>> GetParametersAsync(ExtractParameterGenerator generator)
        {
            var result = new List<ExtractParameterCollection>();

            await foreach (var collection in generator.GenerateAsync())
            {
                result.Add(collection);
            }

            return result;
        }
    }
}
