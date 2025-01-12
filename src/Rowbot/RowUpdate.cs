using Rowbot.Entities;

namespace Rowbot;

public sealed class RowUpdate<TEntity>
{
    public RowUpdate(TEntity row, params FieldDescriptor[] changedFields) : this(row, (IEnumerable<FieldDescriptor>)changedFields) { }

    public RowUpdate(TEntity row, IEnumerable<FieldDescriptor> changedFields)
    {
        Row = row;
        ChangedFields = changedFields;
    }

    /// <summary>
    /// Entity to be updated.
    /// </summary>
    public TEntity Row { get; }
    /// <summary>
    /// List of changed fields. The write connector can use this to partially update the entity.
    /// </summary>
    public IEnumerable<FieldDescriptor> ChangedFields { get; }
}