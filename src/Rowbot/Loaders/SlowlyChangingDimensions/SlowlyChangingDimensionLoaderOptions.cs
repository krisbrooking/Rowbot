using Rowbot.Entities;

namespace Rowbot.Loaders.SlowlyChangingDimensions
{
    public sealed class SlowlyChangingDimensionLoaderOptions<TTarget>
    {
        internal IFieldSelector<TTarget>? FieldsToUpdateOnDelete { get; private set; }
        public void SetFieldsToUpdateOnDelete(Action<IFieldSelector<TTarget>> fieldSelector)
        {
            FieldsToUpdateOnDelete = new FieldSelector<TTarget>();
            fieldSelector?.Invoke(FieldsToUpdateOnDelete);
        }
        public bool OverrideDeleteWithIsActiveFalse { get; set; }
    }
}
