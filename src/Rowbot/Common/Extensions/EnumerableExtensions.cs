namespace Rowbot.Common.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> ChunkBySum<T>(this IEnumerable<T> source, int maxSize, Func<T, int> selector)
        {
            Func<(List<List<T>> Result, int CurrentSize), T, int, Func<T, int>, (List<List<T>> Result, int CurrentSize)> chunker = (accumulator, element, maxSize, selector) =>
            {
                if (selector(element) > maxSize)
                {
                    return (accumulator.Result, accumulator.CurrentSize);
                }

                if (accumulator.CurrentSize + selector(element) > maxSize || 
                    accumulator.Result.Count == 0)
                {
                    accumulator.Result.Add(new List<T>() { element });
                    accumulator.CurrentSize = selector(element);
                }
                else
                {
                    accumulator.CurrentSize += selector(element);
                    accumulator.Result[accumulator.Result.Count - 1].Add(element);
                }

                return (accumulator.Result, accumulator.CurrentSize);
            };

            return source.Aggregate((Result: new List<List<T>>(), CurrentSize: 0), (accumulator, element) => chunker(accumulator, element, maxSize, selector)).Result;
        }

        public static IEnumerable<IEnumerable<T>> Pivot<T>(this IEnumerable<IEnumerable<T>> source)
        {
            var allEnumerators = source.Select(x => x.GetEnumerator()).ToList();

            try
            {
                var currentEnumerators = allEnumerators.Where(x => x.MoveNext()).ToList();

                while (currentEnumerators.Any())
                {
                    var result = currentEnumerators.Select(x => x.Current).ToList();
                    yield return result;
                    currentEnumerators = currentEnumerators.Where(x => x.MoveNext()).ToList();
                }
            }
            finally
            {
                allEnumerators.ForEach(x => x.Dispose());
            }
        }
    }
}
