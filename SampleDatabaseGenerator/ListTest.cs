using System.Collections.Generic;
using Realms;

namespace SampleDatabaseGenerator
{
    public class ListTest : RealmObject
    {
        public IList<int> IntList { get; }
        public IList<string> StringList { get; }
        public IList<Currency> CurrencyList { get; }
    }
}
