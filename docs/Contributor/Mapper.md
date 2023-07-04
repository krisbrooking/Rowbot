# Mapper

## Design
A data pipeline will typically require a mapping step, where data is mapped from one data type to another. This operation is so common that a mapper is built into the default transformer.

A mapper is described using `MapperConfiguration` and applied to an entity using `Mapper`.

### Mapper Configuration
The design of `MapperConfiguration` is inspired by Serilog's LoggerConfiguration, primarily in the way that it provides extension points to external assemblies.

The `MapperConfiguration` api provides two extension points.
- **Map**: For copying values between entities. Currently supports copying the value of one property to another property but could be extended to, for example, automatically copy property values from one entity to another if property names match.
- **Transform**: For transforming the value of an entity. Currently supports generating a hash code and applying a constant value.

### Mapper
A `Mapper` requires a `MapperConfiguration` as a constructor parameter. On creation, a `Mapper` uses the configuration to create a list of mapper actions which are generated as expression trees and compiled into action delegates.

Mapper actions are applied to an entity by invoking an `Apply()` method on the `Mapper` object.

There are two types of mapper action.

| Mapper Action | Action Type | Description |
|----|----|----|
| **Source** | `Action<TSource, TTarget>` | A source mapper action transforms a source to a target field. |
| **Target** | `Action<TTarget>` | A target mapper action transforms a target field in place. |

Mappers are applied in the order, source then target. This way source mapper actions like property value mappers are applied before target mapper actions like hash code generation.

### Usage Recommendations
Mappers are sometimes frowned upon because they hide logic in configuration or convention. Rowbot includes the `MapperConfiguration` in the default `Transform()` method of the pipeline builder and recommends using this approach to keep mapper configuration front and centre of the pipeline.

## Extensibility - Create a Constant Value Transform
Custom transforms are designed to be simple to plug-in to the `Transform` property of `MapperConfiguration`.

### 1. Create Extension Method

The extension method must include a receiver of type `TransformMapperConfiguration<TSource, TTarget>` and return a `MapperConfiguration<TSource, TTarget>`.

Before returning, the extension method should add a mapper to the `MapperConfiguration`. The constant value transform does not operate on the source entity so we can use a `TargetTransformAction<TTarget, TTarget>`.

The mapper holds a lambda expression `Action<TTarget>` which performs the transform. In this example, the lambda expression simply assigns a constant value to a property of `TTarget`.

```csharp
public static class ConstantValueTransformExtensions
{
    public static MapperConfiguration<TSource, TTarget> ToConstantValue<TSource, TTarget, TProperty>(
        this TransformMapperConfiguration<TSource, TTarget> configuration,
        Expression<Func<TTarget, TProperty>> targetPropertySelector,
        TProperty constantValue)
    {
        MemberExpression targetMemberExpression = Ensure.ArgumentIsMemberExpression(targetPropertySelector);

        var mapper = new TargetTransformAction<TTarget, TTarget>(
            GetConstantValueExpression<TTarget, TProperty>(targetMemberExpression, constantValue));

        return configuration.AddTarget(mapper);
    }

    private static Expression<Action<TTarget>> GetConstantValueExpression<TTarget, TProperty>(
        MemberExpression targetMemberExpression, 
        TProperty constantValue)
    {
        var targetParameter = Expression.Parameter(typeof(TTarget), "target");
        var propertyAccessor = Expression.MakeMemberAccess(targetParameter, targetMemberExpression.Member);
        var body = Expression.Assign(propertyAccessor, Expression.Constant(constantValue));

        return Expression.Lambda<Action<TTarget>>(body, targetParameter);
    }
}
```

#### Usage
The extension method makes `ToConstantValue()` accessible from the `Transform` property of `MapperConfiguration`. 

```csharp
var configuration = new MapperConfiguration<SourceCustomer, TargetCustomer>();
configuration.Transform.ToConstantValue(target => target.CreationDate, DateTime.UtcNow);
```