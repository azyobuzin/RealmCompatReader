using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using Realms;

namespace SampleDatabaseGenerator
{
    class Program
    {
        public static void Main(string[] args)
        {
            // System.Collections への参照を残す
            _ = new List<int>();

            var fileName = args.Length > 0 ? args[0] : "default.realm";
            var config = new RealmConfiguration(Path.Combine(Directory.GetCurrentDirectory(), fileName));

            Realm.DeleteRealm(config);

            using (var realm = Realm.GetInstance(config))
            {
                realm.Write(() =>
                {
                    using (var csv = OpenCsv("Currency"))
                    {
                        while (csv.Read())
                        {
                            realm.Add(new Currency()
                            {
                                CurrencyCode = csv[0],
                                Name = csv[1],
                                ModifiedDate = ParseAsUtcDate(csv[2]),
                            });
                        }
                    }

                    using (var csv = OpenCsv("CurrencyRate"))
                    {
                        while (csv.Read())
                        {
                            realm.Add(new CurrencyRate()
                            {
                                CurrencyRateId = csv.GetField<int>(0),
                                CurrencyRateDate = ParseAsUtcDate(csv[1]),
                                FromCurrency = realm.Find<Currency>(csv[2]),
                                ToCurrency = realm.Find<Currency>(csv[3]),
                                AverageRate = csv.GetField<double>(4),
                                EndOfDayRate = csv.GetField<double>(5),
                                ModifiedDate = ParseAsUtcDate(csv[6]),
                            });
                        }
                    }

                    using (var csv = OpenCsv("CountryRegion"))
                    {
                        while (csv.Read())
                        {
                            realm.Add(new CountryRegion()
                            {
                                CountryRegionCode = csv[0],
                                Name = csv[1],
                                ModifiedDate = ParseAsUtcDate(csv[2]),
                            });
                        }
                    }

                    using (var csv = OpenCsv("CountryRegionCurrency"))
                    {
                        while (csv.Read())
                        {
                            realm.Add(new CountryRegionCurrency()
                            {
                                CountryRegion = realm.Find<CountryRegion>(csv[0]),
                                Currency = realm.Find<Currency>(csv[1]),
                                ModifiedDate = ParseAsUtcDate(csv[2]),
                            });
                        }
                    }

                    var listTest = new ListTest()
                    {
                        IntList = { 1, 2, 3 },
                        StringList = { "str", "test" },
                    };
                    foreach (var currency in realm.All<Currency>().AsEnumerable().Take(3))
                        listTest.CurrencyList.Add(currency);
                    realm.Add(listTest);
                });
            }
        }

        private static CsvReader OpenCsv(string name)
        {
            var asmDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var sr = new StreamReader(Path.Combine(asmDirectory, "AdventureWorks", name + ".csv"));
            return new CsvReader(sr, false)
            {
                Configuration =
                {
                    HasHeaderRecord = false,
                    Delimiter = "\t",
                }
            };
        }

        private static DateTimeOffset ParseAsUtcDate(string s)
        {
            return DateTimeOffset.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
        }
    }
}
