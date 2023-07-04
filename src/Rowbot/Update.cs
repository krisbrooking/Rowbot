using Rowbot.Entities;

namespace Rowbot
{
    public sealed class Update<TEntity>
    {
        public Update(TEntity row, params FieldDescriptor[] changedFields) : this(row, (IEnumerable<FieldDescriptor>)changedFields) { }

        public Update(TEntity row, IEnumerable<FieldDescriptor> changedFields)
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
}
