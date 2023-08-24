using Rowbot.Entities;
using Rowbot.Framework.Pipelines.Options;

namespace Rowbot.Loaders.SlowlyChangingDimensions
{
    public sealed class SlowlyChangingDimensionLoaderOptions<TTarget> : LoaderOptions
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
