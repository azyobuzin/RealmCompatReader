using System;
using System.Linq;
using Realms;

namespace SampleDatabaseGenerator
{
    public class Currency : RealmObject
    {
        [PrimaryKey, Required]
        public string CurrencyCode { get; set; }

        [Required]
        public string Name { get; set; }

        public DateTimeOffset ModifiedDate { get; set; }

        [Backlink(nameof(CountryRegionCurrency.Currency))]
        public IQueryable<CountryRegionCurrency> CountryRegions { get; }
    }
}
