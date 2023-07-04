using Rowbot.Common;
using Rowbot.Entities;
using System.Globalization;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text.Json;

namespace Rowbot.Framework.Blocks.Transformers.Mappers.Transforms
{
    /// <summary>
    /// <para>
    /// Builder for selecting one or more properties of an entity.
    /// </para>
    /// <para>
    /// Use <see cref="IHashCodeSelection{T}.Build"/> to create a delegate that computes a hash code.
    /// Invoke the delegate, passing in an instance of <typeparamref name="T"/> to generate the hash
    /// code with the current values of selected properties for that instance.
    /// </para>
    /// </summary>
    public interface IHashCodeTransform<T>
    {
        /// <inheritdoc cref="HashCodeTransform{T}.Include{TProperty}(Expression{Func{T, TProperty}})"/>
        IHashCodeSingleSelectorTransform<T> Include<TProperty>(Expression<Func<T, TProperty>> propertySelector);
        /// <inheritdoc cref="HashCodeTransform{T}.All"/>
        IHashCodeSelection<T> All();
        /// <inheritdoc cref="HashCodeTransform{T}.WithSeed(string, int)"/>
        IHashCodeTransform<T> WithSeed(string key, int value);
        /// <inheritdoc cref="HashCodeTransform{T}.WithSeed(string, int)"/>
        IHashCodeTransform<T> WithSeed(int seed);
    }

    public interface IHashCodeSingleSelectorTransform<T> : IHashCodeSelection<T>
    {
        /// <inheritdoc cref="HashCodeTransform{T}.Include{TProperty}(Expression{Func{T, TProperty}})"/>
        IHashCodeSingleSelectorTransform<T> Include<TProperty>(Expression<Func<T, TProperty>> propertySelector);
    }

    public interface IHashCodeSelection<T>
    {
        /// <summary>
        /// Build a delegate that computes a hash code for the selected properties
        /// </summary>
        Func<T, byte[]> Build();
        /// <summary>
        /// Collection of selected properties
        /// </summary>
        IEnumerable<string> Selected { get; }
        IHashCodeSelection<T> Exclude<TProperty>(Expression<Func<T, TProperty>> propertySelector);
    }

    public sealed class HashCodeTransform<T> : IHashCodeTransform<T>, IHashCodeSingleSelectorTransform<T>, IHashCodeSelection<T>
    {
        private readonly Dictionary<string, Func<T, string>> _hashCodeProperties;
        private readonly List<string> _propertiesToIgnore;
        private readonly JsonSerializerOptions _serializerOptions;
        private readonly EntityDescriptor<T> _entityDescriptor;

        public HashCodeTransform()
        {
            _hashCodeProperties = new Dictionary<string, Func<T, string>>();
            _propertiesToIgnore = new List<string>()
            {
                nameof(Row.KeyHash),
                nameof(Row.ChangeHash),
                nameof(Row.IsDeleted),
                nameof(Row.KeyHashBase64),
                nameof(Row.ChangeHashBase64),
                nameof(Fact.Created),
                nameof(Dimension.FromDateKey),
                nameof(Dimension.FromDate),
                nameof(Dimension.ToDate),
                nameof(Dimension.ToDateKey),
                nameof(Dimension.IsActive)
            };
            _serializerOptions = new JsonSerializerOptions();
            _entityDescriptor = new EntityDescriptor<T>();
        }

        /// <summary>
        /// Selects a single property
        /// </summary>
        public IHashCodeSingleSelectorTransform<T> Include<TProperty>(Expression<Func<T, TProperty>> propertySelector)
        {
            MemberExpression memberExpression = Ensure.ArgumentIsMemberExpression(propertySelector);

            if (_entityDescriptor.Fields.Select(x => x.Property).Any(x => x.Name == memberExpression.Member.Name) &&
                !_propertiesToIgnore.Contains(memberExpression.Member.Name) &&
                !_hashCodeProperties.ContainsKey(memberExpression.Member.Name))
            {
                var getValue = propertySelector.Compile();

                _hashCodeProperties.Add(
                    memberExpression.Member.Name,
                    (source) => JsonSerializer.Serialize(GetCultureInvariantString(getValue(source)), typeof(string), _serializerOptions)
                );
            }

            return this;
        }

        /// <summary>
        /// Selects all properties in <typeparamref name="T"/>
        /// </summary>
        public IHashCodeSelection<T> All()
        {
            foreach (var property in _entityDescriptor.Fields
                .Select(x => x.Property)
                .Where(x => !_propertiesToIgnore.Contains(x.Name) && !_hashCodeProperties.ContainsKey(x.Name)))
            {
                var fieldDescriptor = new FieldDescriptor(property);
                var valueAccessor = EntityAccessor<T>.BuildValueAccessorExpression(fieldDescriptor).Compile();

                _hashCodeProperties.Add(
                    property.Name,
                    (source) => JsonSerializer.Serialize(GetCultureInvariantString(valueAccessor(source)), typeof(string), _serializerOptions)
                );
            }

            return this;
        }

        /// <summary>
        /// Removes a single property
        /// </summary>
        public IHashCodeSelection<T> Exclude<TProperty>(Expression<Func<T, TProperty>> propertySelector)
        {
            MemberExpression memberExpression = Ensure.ArgumentIsMemberExpression(propertySelector);

            if (_hashCodeProperties.ContainsKey(memberExpression.Member.Name))
            {
                _hashCodeProperties.Remove(memberExpression.Member.Name);
            }

            return this;
        }

        /// <summary>
        /// Add a seed value to the hash code so that different sources with the same properties
        /// and values will generate different hash codes.
        /// </summary>
        public IHashCodeTransform<T> WithSeed(string key, int value)
        {
            if (!_hashCodeProperties.ContainsKey(key))
            {
                _hashCodeProperties.Add(
                    key,
                    _ => JsonSerializer.Serialize(GetCultureInvariantString(value), typeof(string), _serializerOptions)
                );
            }

            return this;
        }

        public IHashCodeTransform<T> WithSeed(int seed) => WithSeed("Seed", seed);

        Func<T, byte[]> IHashCodeSelection<T>.Build()
        {
            return source =>
            {
                using (var stream = new MemoryStream())
                using (var writer = new Utf8JsonWriter(stream))
                using (var sha1 = SHA1.Create())
                {
                    writer.WriteStartObject();

                    var values = _hashCodeProperties
                        .OrderBy(x => x.Key)
                        .Select(x => x.Value(source))
                        .ToArray();
                    var array = $"[{string.Join(',', values)}]";

                    writer.WritePropertyName("h");
                    writer.WriteRawValue(array);

                    writer.WriteEndObject();
                    writer.Flush();

                    return sha1.ComputeHash(stream.ToArray());
                }
            };
        }

        IEnumerable<string> IHashCodeSelection<T>.Selected => _hashCodeProperties.Keys;

        private string GetCultureInvariantString(object? value) =>
            Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }
}
