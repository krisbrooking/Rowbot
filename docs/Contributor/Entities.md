# EntityDescriptor

## Design
`EntityDescriptor` describes an entity and provides metadata to other components.

An `EntityDescriptor` is essentially a creator of field descriptors. It exists separately for consistency and performance.
- **Consistency**: `EntityDescriptor` describes an entity while `FieldDescriptor` describes a field.
- **Performance**: Some fields require scanning the entire entity object to find additional object members. An example is foreign key fields. This reflection could be done every time a `FieldDescriptor` is instantiated but it is much faster to find all object members once, cache them, and use the cache when instantiating each individual `FieldDescriptor`.

# EntityAccessor

## Design
`EntityAccessor` provides generic methods for getting and setting values of a field.

On instantiation, `EntityAccessor` builds mappers and accessors for every entity object member. All are cached and reused.

Because entities and fields are generic, mappers and accessors must be generated at runtime. Values of a field could be accessed via reflection but this isn't a viable solution if we are concerned about performance. The accessor could potentially be invoked thousands or millions of times for every run of the data pipeline.

A better approach is to dynamically generate the accessor using expression trees. After the expression tree is compiled and cached, the time taken to invoke the accessor is only slightly slower than if it had been hard coded. Importantly, this approach is much faster than accessing the value via reflection.

# EntityComparer

## Design
`EntityComparer` provides generic methods for comparing the value of a field between two entities.

On instantiation, `EntityComparer` builds comparers for every entity object member. All are cached and reused.

Different data types require different methods to test equality. 
- **Primitive types + string** can be compared using `obj1.Equals(obj2)`
- **Arrays** are compared using `Enumerable<T>.SequenceEqual(obj1, obj2)`
- **Types implementing `IEquatable<T>`** can be compared using the default comparer `EqualityComparer<T>.Default.Equals(obj1, obj2)`

`EntityComparer` dynamically generates comparers as expression trees at runtime based on the data type of the field. This isn't necessarily required. We could instead hard code every method of equality and use a conditional expression to determine which comparer to use. 

Generating comparers at runtime results in cleaner code and maintains consistency (the architecture is similar to that of `EntityAccessor`).