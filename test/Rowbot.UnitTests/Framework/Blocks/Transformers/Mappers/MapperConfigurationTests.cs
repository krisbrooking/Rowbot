using Rowbot.Common;
using Rowbot.Framework.Blocks.Transformers.Mappers.Actions;
using Rowbot.Framework.Blocks.Transformers.Mappers.Configuration;
using Rowbot.UnitTests.Setup;
using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Rowbot.UnitTests.Framework.Blocks.Transformers.Mapper
{
    public class MapperConfigurationTests
    {
        [Fact]
        public void Property_Should_CreateMapper()
        {
            var configuration = new MapperConfiguration<SourcePerson, TargetPerson>();
            configuration.Map.Property(source => source.Id, target => target.Id);

            var source = SourcePerson.GetValidEntity();
            var target = new TargetPerson();

            configuration.BuildSourceMappers().First(x => x.ActionType == SourceMapperActionType.Property).Apply(source, target);

            Assert.Equal(1, target.Id);
        }

        [Fact]
        public void Property_Should_ThrowArgumentException_ForInvalidSelector()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var configuration = new MapperConfiguration<SourcePerson, TargetPerson>();
                configuration.Map.Property(source => source.Id == 1, target => target.Id == 1);
            });
        }

        [Fact]
        public void Transform_Should_CreateMapper()
        {
            var configuration = new MapperConfiguration<SourcePerson, TargetPerson>();
            configuration.Transform.ToHashCode(hash => hash.Include(x => x.Id), x => x.KeyHash);

            var source = SourcePerson.GetValidEntity();
            var target = new TargetPerson() { Id = 1 };

            configuration.BuildTargetMappers().First(x => x.ActionType == TargetMapperActionType.Transform).Apply(target);

            Assert.Equal("2kN1yKJgI+jtps3pmLygD+Dy1Vo=", target.KeyHashBase64);
        }

        [Fact]
        public void MapperConfiguration_Should_CreateMapper_ForCustomTransform()
        {
            var configuration = new MapperConfiguration<SourcePerson, TargetPerson>();
            configuration.Transform.ToObfuscatedString(source => source.First_Name!, target => target.FirstName!);

            var mapperActions = configuration.BuildSourceMappers().ToList();

            var source = SourcePerson.GetValidEntity();
            var target = new TargetPerson();
            mapperActions.First(x => x.ActionType == SourceMapperActionType.Transform).Apply(source, target);

            Assert.Equal("*****", target.FirstName);
        }
    }

    public static class MapperExtensions
    {
        public static MapperConfiguration<TSource, TTarget> ToObfuscatedString<TSource, TTarget>(
            this TransformMapperConfiguration<TSource, TTarget> configuration,
            Expression<Func<TSource, string>> sourcePropertySelector,
            Expression<Func<TTarget, string>> targetPropertySelector)
        {
            MemberExpression sourceMemberExpression = Ensure.ArgumentIsMemberExpression(sourcePropertySelector);
            MemberExpression targetMemberExpression = Ensure.ArgumentIsMemberExpression(targetPropertySelector);

            var mapper = new SourceTransformAction<TSource, TTarget>(GetObfuscateStringExpression<TSource, TTarget>(sourceMemberExpression, targetMemberExpression));

            return configuration.AddSource(mapper);
        }

        internal static Expression<Action<TSource, TTarget>> GetObfuscateStringExpression<TSource, TTarget>(MemberExpression sourcePropertyExpression, MemberExpression targetPropertyExpression)
        {
            var sourceTypeParameter = Expression.Parameter(typeof(TSource), "source");
            var targetTypeParameter = Expression.Parameter(typeof(TTarget), "target");

            var stringLengthProperty = typeof(string).GetProperty("Length");

            var sourceProperty = sourcePropertyExpression.Member;
            var sourcePropertyAccessExpression = Expression.MakeMemberAccess(sourceTypeParameter, sourceProperty);
            var sourcePropertyLengthAccessExpression = Expression.MakeMemberAccess(sourcePropertyAccessExpression, stringLengthProperty!);

            var targetProperty = targetPropertyExpression.Member;
            var targetPropertyAccessExpression = Expression.MakeMemberAccess(targetTypeParameter, targetProperty);

            var newStringExpression = Expression.New(typeof(string).GetConstructor(new[] { typeof(char), typeof(int) })!, Expression.Constant('*'), sourcePropertyLengthAccessExpression);

            var body = Expression.Assign(targetPropertyAccessExpression, newStringExpression);
            var lambda = Expression.Lambda<Action<TSource, TTarget>>(body, sourceTypeParameter, targetTypeParameter);

            return lambda;
        }
    }
}
