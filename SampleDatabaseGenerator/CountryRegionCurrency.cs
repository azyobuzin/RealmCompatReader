using System;
using Realms;

namespace SampleDatabaseGenerator
{
    public class CountryRegionCurrency : RealmObject
    {
        public CountryRegion CountryRegion { get; set; }

        public Currency Currency { get; set; }

        public DateTimeOffset ModifiedDate { get; set; }
    }
}
