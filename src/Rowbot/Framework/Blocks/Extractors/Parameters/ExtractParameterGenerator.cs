namespace Rowbot.Framework.Blocks.Extractors.Parameters
{
    public sealed class ExtractParameterGenerator
    {
        private readonly Queue<ExtractParameterFactory> _factoryQueue;

        public ExtractParameterGenerator() 
        { 
            _factoryQueue = new();
        }        

        public void Add(ExtractParameter parameter) => _factoryQueue.Enqueue(new ExtractParameterFactory(parameter));
        public void Add(ExtractParameterCollection collection) => _factoryQueue.Enqueue(new ExtractParameterFactory(collection));
        public void Add(Func<IEnumerable<ExtractParameterCollection>> synchronousFactory) => _factoryQueue.Enqueue(new ExtractParameterFactory(synchronousFactory));
        public void Add(Func<Task<IEnumerable<ExtractParameterCollection>>> asynchronousFactory) => _factoryQueue.Enqueue(new ExtractParameterFactory(asynchronousFactory));

        public async IAsyncEnumerable<ExtractParameterCollection> GenerateAsync()
        {
            if (_factoryQueue.Count == 0)
            {
                yield return new ExtractParameterCollection();
            }
            else
            {
                while (_factoryQueue.Count > 0 && _factoryQueue.Peek() is not null)
                {
                    await foreach (var collection in _factoryQueue.Dequeue().GetParametersAsync())
                    {
                        yield return collection;
                    }
                }
            }
        }
    }
}
