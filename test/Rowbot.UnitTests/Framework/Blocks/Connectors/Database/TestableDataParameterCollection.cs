using System.Collections.Generic;
using System.Data;

namespace Rowbot.UnitTests.Framework.Blocks.Connectors.Database
{
    public sealed class TestableDataParameterCollection : List<TestableDataParameter>, IDataParameterCollection
    {
        public TestableDataParameterCollection() : base() { }

        public object this[string parameterName]
        {
            get
            {
                return this[IndexOf(parameterName)];
            }
            set
            {
                this[IndexOf(parameterName)] = (TestableDataParameter)value;
            }
        }

        public bool Contains(string parameterName) => IndexOf(parameterName) != -1;

        public int IndexOf(string parameterName)
        {
            var index = 0;
            foreach (TestableDataParameter parameter in this)
            {
                if (parameter.ParameterName.Equals(parameterName))
                {
                    return index;
                }
                index++;
            }

            return -1;
        }

        public void RemoveAt(string parameterName) => RemoveAt(IndexOf(parameterName));
    }
}
