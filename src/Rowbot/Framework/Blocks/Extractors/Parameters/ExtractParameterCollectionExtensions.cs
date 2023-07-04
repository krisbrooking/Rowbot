namespace Rowbot.Framework.Blocks.Extractors.Parameters
{
    public static class ExtractParameterCollectionExtensions
    {
        internal static ExtractParameterCollection AddBatchSizeParameter(this ExtractParameterCollection collection, int batchSize)
        {
            var batchSizeParameter = new ExtractParameter("BatchSize", typeof(int), batchSize);

            if (!collection.Any(x => x.ParameterName == "BatchSize"))
            {
                collection.Add(batchSizeParameter);
            }

            return collection;
        }
    }
}
