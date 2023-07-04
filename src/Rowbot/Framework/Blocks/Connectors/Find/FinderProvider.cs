using Rowbot.Common;
using Rowbot.Entities;
using System.Security.Cryptography;
using System.Text;

namespace Rowbot.Framework.Blocks.Connectors.Find
{
    /// <summary>
    /// Creates instances of <see cref="Finder{TEntity}"/> which provide helper methods for simple implementations of 
    /// <see cref="IWriteConnector{TEntity}.FindAsync(IEnumerable{TEntity}, Action{IFieldSelector{TEntity}}, Action{IFieldSelector{TEntity}})"/>
    /// including predicate for comparison and mappers for results.
    /// </summary>
    public interface IFinderProvider
    {
        Finder<TEntity> CreateFinder<TEntity>(Action<IFieldSelector<TEntity>> compareSelector, Action<IFieldSelector<TEntity>> resultSelector, EntityComparer<TEntity> entityComparer);
    }

    /// <inheritdoc cref="IFinderProvider"/>
    public sealed class FinderProvider : IFinderProvider
    {
        private readonly Dictionary<string, IFinder> _cache;

        /// <inheritdoc cref="FinderProvider"/>
        public FinderProvider()
        {
            _cache = new Dictionary<string, IFinder>();
        }

        /// <summary>
        /// Creates a finder (or gets it from cache)
        /// </summary>
        public Finder<TEntity> CreateFinder<TEntity>(Action<IFieldSelector<TEntity>> compareSelector, Action<IFieldSelector<TEntity>> resultSelector, EntityComparer<TEntity> entityComparer)
        {
            Ensure.ArgumentIsNotNull(compareSelector);
            Ensure.ArgumentIsNotNull(resultSelector);

            var findFieldSelector = new FieldSelector<TEntity>();
            compareSelector(findFieldSelector);

            var resultFieldSelector = new FieldSelector<TEntity>();
            resultSelector(resultFieldSelector);

            var finderHashCode = GenerateHashCode(findFieldSelector.Selected, resultFieldSelector.Selected);
            if (_cache.ContainsKey(finderHashCode))
            {
                return (Finder<TEntity>)_cache[finderHashCode];
            }

            var finder = new Finder<TEntity>(findFieldSelector, resultFieldSelector, entityComparer);
            _cache.Add(finderHashCode, finder);

            return finder;
        }

        internal string GenerateHashCode(IEnumerable<FieldDescriptor> findFields, IEnumerable<FieldDescriptor> resultFields)
        {
            var findProperties = findFields.Select(x => x.Property.Name).OrderBy(x => x);
            var resultProperties = resultFields.Select(x => x.Property.Name).OrderBy(x => x);

            using (var stream = new MemoryStream())
            using (var sha1 = SHA1.Create())
            {
                var findBytes = Encoding.UTF8.GetBytes($"Find:{string.Join(',', findProperties)})");
                stream.Write(findBytes, 0, findBytes.Length);

                var resultBytes = Encoding.UTF8.GetBytes($"Result:{string.Join(',', resultProperties)})");
                stream.Write(resultBytes, 0, resultBytes.Length);

                var hash = sha1.ComputeHash(stream.ToArray());
                return Convert.ToBase64String(hash);
            }
        }
    }
}
