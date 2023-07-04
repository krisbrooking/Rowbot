using Rowbot.Common;
using Rowbot.UnitTests.Setup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;

namespace Rowbot.UnitTests.Common
{
    public class EnsureTests
    {
        [Fact]
        public void ArgumentIsNotNull_Should_ReturnObjectInstance_WhenObjectIsNotNull()
        {
            var source = SourcePerson.GetValidEntity();
            var nonNullSource = Ensure.ArgumentIsNotNull(source);

            Assert.Same(source, nonNullSource);
        }

        [Fact]
        public void ArgumentIsNotNull_Should_ThrowArgumentException_WhenObjectIsNull()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                Ensure.ArgumentIsNotNull((string)null!);
            });
        }

        [Fact]
        public void ArgumentIsNotNull_Should_ThrowArgumentException_WhenStringIsEmpty()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                Ensure.ArgumentIsNotNull(string.Empty);
            });
        }

        [Fact]
        public void ArgumentIsMemberExpression_Should_ReturnMemberExpression_WhenArgumentIsMemberExpression()
        {
            Expression<Func<SourcePerson, int>> selector = x => x.Id;
            var memberExpression = Ensure.ArgumentIsMemberExpression(selector);

            Assert.IsAssignableFrom<MemberExpression>(memberExpression);
        }

        [Fact]
        public void ArgumentIsMemberExpression_Should_ThrowArgumentException_WhenArgumentIsNotMemberExpression()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                Expression<Func<SourcePerson, bool>> selector = x => x.Id == 1;
                var memberExpression = Ensure.ArgumentIsMemberExpression(selector);
            });
        }

        [Fact]
        public void ArgumentIsMemberInitExpression_Should_ReturnMemberInitExpression_WhenArgumentIsMemberInitExpression()
        {
            Expression<Func<SourcePerson, TargetPerson>> selector = x => new TargetPerson() { Id = x.Id };
            var memberExpression = Ensure.ArgumentIsMemberInitExpression(selector);

            Assert.IsAssignableFrom<MemberInitExpression>(memberExpression);
        }

        [Fact]
        public void ArgumentIsMemberInitExpression_Should_ThrowArgumentException_WhenArgumentIsNotMemberInitExpression()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                Expression<Func<SourcePerson, TargetPerson>> selector = x => new TargetPerson(x.Id, x.First_Name, x.Last_Name);
                var memberExpression = Ensure.ArgumentIsMemberInitExpression(selector);
            });
        }

        [Fact]
        public void MemberExpressionTargetsProperty_Should_ReturnPropertyInfo_WhenArgumentIsMemberExpression()
        {
            Expression<Func<SourcePerson, int>> selector = x => x.Id;
            var memberExpression = Ensure.ArgumentIsMemberExpression(selector);
            var propertyInfo = Ensure.MemberExpressionTargetsProperty(memberExpression);

            Assert.IsAssignableFrom<PropertyInfo>(propertyInfo);
        }

        [Fact]
        public void MemberExpressionTargetsProperty_Should_ThrowArgumentException_WhenArgumentIsNotMemberExpression()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                Expression<Func<AnnotatedEntityWithPublicFields, string>> selector = x => x.PublicField;
                var memberExpression = Ensure.ArgumentIsMemberExpression(selector);
                Ensure.MemberExpressionTargetsProperty((selector.Body as MemberExpression)!);
            });
        }

        [Fact]
        public void SelectorsTargetTheSamePropertyType_Should_NotThrowException_WhenPropertyTypesMatch()
        {
            Expression<Func<SourcePerson, string>> sourceSelector = x => x.First_Name!;
            Expression<Func<TargetPerson, string>> targetSelector = x => x.FirstName!;

            Ensure.SelectorsTargetTheSamePropertyType((MemberExpression)sourceSelector.Body, (MemberExpression)targetSelector.Body);

            Assert.True(true);
        }

        [Fact]
        public void SelectorsTargetTheSamePropertyType_Should_ThrowInvalidOperationException_WhenPropertyTypesDiffer()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                Expression<Func<SourcePerson, int>> sourceSelector = x => x.Id;
                Expression<Func<TargetPerson, string>> targetSelector = x => x.FirstName!;

                Ensure.SelectorsTargetTheSamePropertyType((MemberExpression)sourceSelector.Body, (MemberExpression)targetSelector.Body);
            });
        }

        [Fact]
        public void KeyExistsInCollection_Should_ReturnItem_WhenItemExistsInEnumerable()
        {
            var people = SourcePerson.GetValidEntities();
            var person = Ensure.ItemExistsInCollection(people, x => string.Equals(x.First_Name, "Alice", StringComparison.OrdinalIgnoreCase));

            Assert.Equal(1, person.Id);
        }

        [Fact]
        public void KeyExistsInCollection_Should_ThrowKeyNotFoundException_WhenItemDoesNotExistInEnumerable()
        {
            Assert.Throws<KeyNotFoundException>(() =>
            {
                var people = SourcePerson.GetValidEntities();
                Ensure.ItemExistsInCollection(people, x => x.Id == 3);
            });
        }

        [Fact]
        public void KeyExistsInCollection_Should_ReturnItem_WhenItemExistsInDictionary()
        {
            var people = SourcePerson.GetValidEntities().ToDictionary(x => x.Id, x => x);
            var person = Ensure.ItemExistsInCollection(people, 1);

            Assert.Equal("Alice", person.First_Name);
        }

        [Fact]
        public void KeyExistsInCollection_Should_ThrowKeyNotFoundException_WhenItemDoesNotExistInDictionary()
        {
            Assert.Throws<KeyNotFoundException>(() =>
            {
                var people = SourcePerson.GetValidEntities().ToDictionary(x => x.Id, x => x);
                Ensure.ItemExistsInCollection(people, 3);
            });
        }
    }
}
