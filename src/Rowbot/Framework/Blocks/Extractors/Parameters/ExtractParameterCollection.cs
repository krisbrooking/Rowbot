using System.Collections.ObjectModel;

namespace Rowbot
{
    public sealed class ExtractParameterCollection : Collection<ExtractParameter>
    {
        public ExtractParameterCollection(params ExtractParameter[] extractParameters)
        {
            foreach (var parameter in extractParameters)
            {
                Items.Add(parameter);
            }
        }

        public ExtractParameterCollection(IEnumerable<ExtractParameter> extractParameters)
        {
            foreach (var parameter in extractParameters)
            {
                Items.Add(parameter);
            }
        }

        public ExtractParameterCollection Concat(ExtractParameterCollection target)
        {
            foreach (var item in target)
            {
                if (!Items.Any(x => x.ParameterName == item.ParameterName))
                {
                    Items.Add(item);
                }
            }

            return new ExtractParameterCollection(Items);
        }

        protected override void InsertItem(int index, ExtractParameter item)
        {
            if (!Items.Any(x => x.ParameterName == item.ParameterName))
            {
                base.InsertItem(index, item);
            }
        }
    }
}
