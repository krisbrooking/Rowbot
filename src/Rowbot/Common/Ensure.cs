using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Rowbot.Common
{
    internal static class Ensure
    {
        /// <summary>
        /// {argument} must contain a value
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public static TArg ArgumentIsNotNull<TArg>(TArg value, [CallerArgumentExpression("value")] string argument = default!)
        {
            if (value is null || value is string stringValue && string.IsNullOrEmpty(stringValue))
            {
                throw new ArgumentException($"{argument} must contain a value");
            }

            return value;
        }

        /// <summary>
        /// {argument} must be a MemberExpression that selects a property. Example: x => x.Id
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public static MemberExpression ArgumentIsMemberExpression(Expression expression, [CallerArgumentExpression("expression")] string argument = default!)
        {
            ArgumentIsNotNull(expression, argument);
            if (expression is not LambdaExpression lambdaExpression ||
                lambdaExpression.Body is not MemberExpression memberExpression)
            {
                throw new ArgumentException($"{argument} must be a MemberExpression that selects a property. Example: x => x.Id");
            }

            return memberExpression;
        }

        /// <summary>
        /// {argument} must be a MemberInitExpression that selects a new object. Example: source => new Target() { Id = source.Id }
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public static MemberInitExpression ArgumentIsMemberInitExpression(Expression expression, [CallerArgumentExpression("expression")] string argument = default!)
        {
            ArgumentIsNotNull(expression, argument);
            if (expression is not LambdaExpression lambdaExpression ||
                lambdaExpression.Body is not MemberInitExpression memberInitExpression)
            {
                throw new ArgumentException($"{argument} must be a MemberInitExpression that selects a new object using a public parameterless constructor. Example: source => new Target() {{Id = source.Id}}");
            }

            return memberInitExpression;
        }

        /// <summary>
        /// {argument} must be a MemberExpression that selects a property. Example: x => x.Id
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public static PropertyInfo MemberExpressionTargetsProperty(MemberExpression expression, [CallerArgumentExpression("expression")] string argument = default!)
        {
            if (expression.Member is not PropertyInfo propertyInfo)
            {
                throw new ArgumentException($"{argument} must be a MemberExpression that selects a property. Example: x => x.Id");
            }

            return propertyInfo;
        }

        /// <summary>
        /// Source property type {sourceProperty} does not match target property type {targetProperty}
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public static void SelectorsTargetTheSamePropertyType(
            MemberExpression sourceExpression,
            MemberExpression targetExpression,
            [CallerArgumentExpression("sourceExpression")] string sourceArgument = default!,
            [CallerArgumentExpression("targetExpression")] string targetArgument = default!)
        {
            var sourceProperty = MemberExpressionTargetsProperty(sourceExpression, sourceArgument);
            var targetProperty = MemberExpressionTargetsProperty(targetExpression, targetArgument);

            if (sourceProperty.PropertyType != targetProperty.PropertyType)
            {
                throw new InvalidOperationException($"Source property type {FriendlyPropertyType(sourceProperty)} does not match target property type {FriendlyPropertyType(targetProperty)}");
            }

            string FriendlyPropertyType(PropertyInfo property) =>
                property.DeclaringType is null
                    ? $"{property.PropertyType} ({property.Name})"
                    : $"{property.PropertyType} ({property.DeclaringType}.{property.Name})";
        }

        /// <summary>
        /// {argument} not found in collection
        /// </summary>
        /// <exception cref="KeyNotFoundException"></exception>
        public static T ItemExistsInCollection<T>(
            IEnumerable<T> collection,
            Func<T, bool> keyPredicate,
            [CallerArgumentExpression("keyPredicate")] string keyPredicateArgument = default!,
            [CallerArgumentExpression("collection")] string collectionArgument = default!)
        {
            if (!collection.Any(keyPredicate))
            {
                throw new KeyNotFoundException($"{keyPredicateArgument} not found in collection {collectionArgument}");
            }

            return collection.First(keyPredicate);
        }

        /// <summary>
        /// {argument} not found in collection
        /// </summary>
        /// <exception cref="KeyNotFoundException"></exception>
        public static TValue ItemExistsInCollection<TKey, TValue>(
            IDictionary<TKey, TValue> collection,
            TKey key,
            [CallerArgumentExpression("collection")] string collectionArgument = default!,
            [CallerArgumentExpression("key")] string keyArgument = default!)
        {
            ArgumentIsNotNull(key);
            if (!collection.ContainsKey(key))
            {
                throw new KeyNotFoundException($"{keyArgument} not found in collection {collectionArgument}");
            }

            return collection[key];
        }
    }
}
