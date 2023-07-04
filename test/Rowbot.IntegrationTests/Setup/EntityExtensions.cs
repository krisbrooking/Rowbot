using Rowbot.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Rowbot.IntegrationTests.Setup
{
    public static class EntityExtensions
    {
        public static IEnumerable<T> Assign<T, TProperty>(
            this IEnumerable<T> source,
            Expression<Func<T, TProperty>> propertyToSet,
            Expression<Func<T, TProperty>> propertyToGet)
        {
            Action<T> mapper = GetPropertyMapper(propertyToSet, propertyToGet);

            foreach (var item in source)
            {
                mapper(item);
            }

            return source;
        }

        public static IEnumerable<T> Assign<T, TProperty>(
            this IEnumerable<T> source,
            Expression<Func<T, TProperty>> propertyToSet,
            TProperty value)
        {
            Action<T> mapper = GetConstantMapper(propertyToSet, value);

            foreach (var item in source)
            {
                mapper(item);
            }

            return source;
        }

        public static IEnumerable<T> AssignWhere<T, TProperty>(
            this IEnumerable<T> source,
            Func<T, bool> predicate,
            Expression<Func<T, TProperty>> propertyToSet,
            Expression<Func<T, TProperty>> propertyToGet)
        {
            Action<T> mapper = GetPropertyMapper(propertyToSet, propertyToGet);

            foreach (var item in source.Where(predicate))
            {
                mapper(item);
            }

            return source;
        }

        public static IEnumerable<T> AssignWhere<T, TProperty>(
            this IEnumerable<T> source,
            Func<T, bool> predicate,
            Expression<Func<T, TProperty>> propertyToSet,
            TProperty value)
        {
            Action<T> mapper = GetConstantMapper(propertyToSet, value);

            foreach (var item in source.Where(predicate))
            {
                mapper(item);
            }

            return source;
        }

        private static Action<T> GetPropertyMapper<T, TProperty>(Expression<Func<T, TProperty>> left, Expression<Func<T, TProperty>> right)
        {
            var leftMember = Ensure.ArgumentIsMemberExpression(left);
            var leftProperty = Ensure.MemberExpressionTargetsProperty(leftMember);
            var rightMember = Ensure.ArgumentIsMemberExpression(right);
            var rightProperty = Ensure.MemberExpressionTargetsProperty(rightMember);

            var sourceParameter = Expression.Parameter(typeof(T), "source");

            var body = Expression.Assign(Expression.MakeMemberAccess(sourceParameter, leftProperty), Expression.MakeMemberAccess(sourceParameter, rightProperty));

            return Expression.Lambda<Action<T>>(body, sourceParameter).Compile();
        }

        private static Action<T> GetConstantMapper<T, TProperty>(Expression<Func<T, TProperty>> left, TProperty right)
        {
            var leftMember = Ensure.ArgumentIsMemberExpression(left);
            var leftProperty = Ensure.MemberExpressionTargetsProperty(leftMember);

            var sourceParameter = Expression.Parameter(typeof(T), "source");

            var body = Expression.Assign(Expression.MakeMemberAccess(sourceParameter, leftProperty), Expression.Convert(Expression.Constant(right), leftProperty.PropertyType));
            return Expression.Lambda<Action<T>>(body, sourceParameter).Compile();
        }
    }
}
