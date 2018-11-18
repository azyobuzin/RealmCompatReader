using System;
using System.Linq;
using Realms;

namespace SampleDatabaseGenerator
{
    public class CountryRegion : RealmObject
    {
        [PrimaryKey, Required]
        public string CountryRegionCode { get; set; }

        [Required]
        public string Name { get; set; }

        public DateTimeOffset ModifiedDate { get; set; }

        [Backlink(nameof(CountryRegionCurrency.CountryRegion))]
        public IQueryable<CountryRegionCurrency> Currencies { get; }
    }
}
