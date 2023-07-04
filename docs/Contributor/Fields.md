# FieldDescriptor

## Design
`FieldDescriptor` describes a field in an entity and provides metadata to other components.

One `FieldDescriptor` is created for every property of an entity object during the creation of the `EntityDescriptor` that represents that object.

The `FieldDescriptor` is essentially an extension of `System.Reflection.PropertyInfo` that additionally provides ETL specific data. The constructor requires a `PropertyInfo` object.

This abstraction allows, for example, an entity object to include a property that is named differently to the field name it represents. 

In the following example, the entity object property is named "Id" and the field is named "CustomerId".
> Note: Field name is almost always used over entity object property name. However, property name is used when accessing a value.

|Key|Value|
|---|---|
|Name|CustomerId|
|PropertyInfo|<table><tr><td>Name</td><td>Id</td></tr><tr><td>PropertyType</td><td>Int32</td></tr></table>|

# FieldSelector

## Design
`FieldSelector` allows the consumer to select one or more field descriptors from an entity.

`FieldSelector` utilises the selector expression: `Expression<Func<TEntity, TField>>`. 

`Expression<Func<TEntity, TField>>` is great for selecting a single property but is less useful when selecting a dynamic number of different property types. For this to work, we need to store each `TField` in a non-generic data structure. Fortunately, we already have `FieldDescriptor` which stores the property type as a `PropertyInfo`.

It, therefore, makes sense to create a builder with `Include<TField>(Expression<Func<TEntity, TField>> fieldSelector)` and chain calls to that method to return an arbitrary number of `FieldDescriptor` objects.

- Example: `selector => selector.Include(x => x.Id).Include(x => x.Name)` translates to `selector => selector.Include<int>(x => x.Id).Include<string>(x => x.Name)`. Note: generic arguments on `Include<>()` are not required thanks to type inference.