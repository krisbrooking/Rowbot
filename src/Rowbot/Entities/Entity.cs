namespace Rowbot.Entities;

/// <summary>
/// <para>
/// Wraps <see cref="EntityDescriptor{TEntity}"/>, <see cref="EntityAccessor{TEntity}"/>, and <see cref="EntityComparer{TEntity}"/>.
/// <see cref="Entity{TEntity}"/> is intended to be passed to and used by a connector.
/// </para>
/// <para>
/// <see cref="EntityDescriptor{TEntity}"/> describes the entity and its fields
/// </para>
/// <para>
/// <see cref="EntityAccessor{TEntity}"/> is used to retrieve values from fields or map values from one entity to another without using reflection
/// </para>
/// <para>
/// <see cref="EntityComparer{TEntity}"/> is used to compare fields from one entity to another without using reflection.
/// </para>
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
public interface IEntity<TEntity>
{
    Lazy<EntityDescriptor<TEntity>> Descriptor { get; }
    Lazy<EntityAccessor<TEntity>> Accessor { get; }
    Lazy<EntityComparer<TEntity>> Comparer { get; }
    HashSet<Type> SupportedDataTypes { get; set; }
}

/// <inheritdoc cref="IEntity{TEntity}"/>
public sealed class Entity<TEntity> : IEntity<TEntity>
{
    public Entity()
    {
        Descriptor = new Lazy<EntityDescriptor<TEntity>>(() => new EntityDescriptor<TEntity>(SupportedDataTypes));
        Accessor = new Lazy<EntityAccessor<TEntity>>(() => new EntityAccessor<TEntity>(Descriptor.Value));
        Comparer = new Lazy<EntityComparer<TEntity>>(() => new EntityComparer<TEntity>(Descriptor.Value));
    }

    public Lazy<EntityDescriptor<TEntity>> Descriptor { get; }
    public Lazy<EntityAccessor<TEntity>> Accessor { get; }
    public Lazy<EntityComparer<TEntity>> Comparer { get; }
    public HashSet<Type> SupportedDataTypes { get; set; } = Entity.CommonDataTypes;
}

internal sealed class Entity
{
    internal static HashSet<Type> CommonDataTypes = new HashSet<Type>
    {
        typeof(bool),
        typeof(byte),
        typeof(byte[]),
        typeof(char),
        typeof(DateOnly),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(decimal),
        typeof(double),
        typeof(Guid),
        typeof(short),
        typeof(int),
        typeof(long),
        typeof(sbyte),
        typeof(float),
        typeof(string),
        typeof(TimeOnly),
        typeof(TimeSpan),
        typeof(ushort),
        typeof(uint),
        typeof(ulong)
    };
}
