using System;
using Realms;

namespace SampleDatabaseGenerator
{
    public class CurrencyRate : RealmObject
    {
        [PrimaryKey]
        public int CurrencyRateId { get; set; }

        public DateTimeOffset CurrencyRateDate { get; set; }

        public Currency FromCurrency { get; set; }

        public Currency ToCurrency { get; set; }

        public double AverageRate { get; set; }

        public double EndOfDayRate { get; set; }

        public DateTimeOffset ModifiedDate { get; set; }
    }
}
