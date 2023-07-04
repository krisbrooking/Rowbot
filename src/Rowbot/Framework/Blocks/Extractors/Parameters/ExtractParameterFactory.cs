namespace Rowbot.Framework.Blocks.Extractors.Parameters
{
    internal sealed class ExtractParameterFactory
    {
        private Func<IEnumerable<ExtractParameterCollection>>? _synchronousFactory;
        private Func<Task<IEnumerable<ExtractParameterCollection>>>? _asynchronousFactory;
        private Func<IAsyncEnumerable<ExtractParameterCollection>>? _asynchronousIteratorFactory;

        public ExtractParameterFactory(ExtractParameter parameter)
        {
            _synchronousFactory = () =>
                new List<ExtractParameterCollection>()
                {
                    new ExtractParameterCollection(parameter)
                };
        }

        public ExtractParameterFactory(ExtractParameterCollection collection)
        {
            _synchronousFactory = () => new List<ExtractParameterCollection>() { collection };
        }

        public ExtractParameterFactory(Func<IEnumerable<ExtractParameterCollection>> synchronousFactory)
        {
            _synchronousFactory = synchronousFactory;
        }

        public ExtractParameterFactory(Func<Task<IEnumerable<ExtractParameterCollection>>> asynchronousFactory)
        {
            _asynchronousFactory = asynchronousFactory;
        }

        public ExtractParameterFactory(Func<IAsyncEnumerable<ExtractParameterCollection>> asynchronousIteratorFactory)
        {
            _asynchronousIteratorFactory = asynchronousIteratorFactory;
        }

        public async IAsyncEnumerable<ExtractParameterCollection> GetParametersAsync()
        {
            if (_synchronousFactory is not null)
            {
                foreach (var collection in _synchronousFactory!.Invoke())
                {
                    yield return collection;
                }
            }
            else if (_asynchronousFactory is not null)
            {
                foreach (var collection in await _asynchronousFactory!.Invoke())
                {
                    yield return collection;
                }
            }
            else if (_asynchronousIteratorFactory is not null)
            {
                await foreach (var collection in _asynchronousIteratorFactory!.Invoke())
                {
                    yield return collection;
                }
            }
            else
            {
                yield return new ExtractParameterCollection();
            }
        }
    }
}
