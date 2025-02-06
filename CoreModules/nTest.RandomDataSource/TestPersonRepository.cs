using NTech.Banking.CivicRegNumbers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nTest.RandomDataSource
{
    public class ComposableTestPersonRepository
    {
        public ComposableTestPersonRepository(string baseCountry) : this(baseCountry, false)
        {

        }

        public ComposableTestPersonRepository(string baseCountry, bool useFiReformedCenturyMarker)
        {
            this.baseCountry = baseCountry;
            this.useFiReformedCenturyMarker = useFiReformedCenturyMarker;
        }

        private const string CivicRegNrCode = "civicRegNr";
        private const string CivicRegNrCountryCode = "civicRegNrCountry";

        //reuseExisting: This is a good idea when the same random seed is reused. Eventually the 10 tries will be hit and it will stop working otherwise
        public StoredPerson GenerateNewTestPerson(bool isAccepted, IRandomnessSource random, IDocumentDatabaseUnitOfWork tr, DateTime now, bool? reuseExisting = false, DateTime? customBirthDate = null, ICivicRegNumber civicRegNr = null, Dictionary<string, string> overrides = null)
        {
            Func<StoredPerson> gen = () =>
            {
                ICivicRegNumber newCivicRegNr;

                if (civicRegNr != null)
                {
                    reuseExisting = true;
                    newCivicRegNr = civicRegNr;
                }
                else
                {
                    newCivicRegNr = TestPersonGenerator.GetSharedInstance(this.baseCountry, useFiReformedCenturyMarker).GenerateCivicRegNumber(random, customBirthDate: customBirthDate);
                }

                Func<StoredPerson, bool> applyOverrides = pp =>
                {
                    if (overrides == null || overrides.Count == 0)
                        return false;
                    foreach (var v in overrides)
                        pp.Properties[v.Key] = v.Value;
                    return true;
                };

                var existingCustomer = GetI(newCivicRegNr.Country, newCivicRegNr.NormalizedValue, tr);
                if (existingCustomer != null)
                {
                    if (reuseExisting.GetValueOrDefault())
                    {
                        if (applyOverrides(existingCustomer))
                        {
                            tr.AddOrUpdate($"{existingCustomer.CivicRegNrTwoLetterCountryIsoCode}#{existingCustomer.CivicRegNr}", StoredPersonCollectionName, existingCustomer);
                        }
                        return existingCustomer;
                    }
                    else
                        return null;
                }

                var newProps = TestPersonGenerator.GetSharedInstance(this.baseCountry).GenerateTestPerson(random, newCivicRegNr, isAccepted, now);

                var person = new StoredPerson
                {
                    CivicRegNr = newCivicRegNr.NormalizedValue,
                    CivicRegNrTwoLetterCountryIsoCode = newCivicRegNr.Country,
                    Properties = newProps
                };
                applyOverrides(person);

                tr.AddOrUpdate($"{person.CivicRegNrTwoLetterCountryIsoCode}#{person.CivicRegNr}", StoredPersonCollectionName, person);

                return person;
            };

            for (var n = 0; n < 10; ++n)
            {
                var newPerson = gen();
                if (newPerson != null)
                    return newPerson;
            }

            throw new Exception("Failed to generate testperson after 10 tries");
        }

        public StoredPerson AddOrUpdate(IDictionary<string, string> properties, bool returnPerson, IDocumentDatabaseUnitOfWork tr)
        {
            var civicRegNr = properties[CivicRegNrCode];
            var civicRegNrTwoLetterCountryIsoCode = properties[CivicRegNrCountryCode];
            if (GetI(civicRegNrTwoLetterCountryIsoCode, civicRegNr, tr) != null)
                Update(properties, tr);
            else
                Add(properties, tr);
            if (returnPerson)
                return GetI(civicRegNrTwoLetterCountryIsoCode, civicRegNr, tr);
            else
                return null;
        }

        public void Add(IDictionary<string, string> properties, IDocumentDatabaseUnitOfWork tr)
        {
            var person = new StoredPerson
            {
                CivicRegNr = properties[CivicRegNrCode],
                CivicRegNrTwoLetterCountryIsoCode = properties[CivicRegNrCountryCode],
                Properties = properties
                    .Where(x => x.Key != CivicRegNrCountryCode && x.Key != CivicRegNrCode)
                    .ToDictionary(x => x.Key.ToString(), x => x.Value)
            };
            tr.AddOrUpdate($"{person.CivicRegNrTwoLetterCountryIsoCode}#{person.CivicRegNr}", StoredPersonCollectionName, person);
        }

        public void Update(IDictionary<string, string> properties, IDocumentDatabaseUnitOfWork tr)
        {
            var civicRegNr = properties[CivicRegNrCode];
            var civicRegNrTwoLetterCountryIsoCode = properties[CivicRegNrCountryCode];
            var result = GetI(civicRegNrTwoLetterCountryIsoCode, civicRegNr, tr);
            if (result == null)
                throw new Exception("No such person!");
            var d = new Dictionary<string, string>();
            foreach (var p in result.Properties)
            {
                d[p.Key] = p.Value;
            }
            foreach (var p in properties)
            {
                d[p.Key] = p.Value;
            }
            result.Properties = properties
                    .Where(x => x.Key != CivicRegNrCountryCode && x.Key != CivicRegNrCode)
                    .ToDictionary(x => x.Key.ToString(), x => x.Value);

            tr.AddOrUpdate($"{result.CivicRegNrTwoLetterCountryIsoCode}#{result.CivicRegNr}", StoredPersonCollectionName, result);
        }

        public IDictionary<string, string> Get(string civicRegNrTwoLetterCountryIsoCode, string civicRegNr, IDocumentDatabaseUnitOfWork tr)
        {
            var result = GetI(civicRegNrTwoLetterCountryIsoCode, civicRegNr, tr);
            if (result == null)
                return null;

            var d = new Dictionary<string, string>();
            d[CivicRegNrCode] = result.CivicRegNr;
            d[CivicRegNrCountryCode] = result.CivicRegNrTwoLetterCountryIsoCode;
            return result.Properties;
        }

        public StoredPerson GetI(string civicRegNrTwoLetterCountryIsoCode, string civicRegNr, IDocumentDatabaseUnitOfWork tr)
        {
            return tr.Get<StoredPerson>($"{civicRegNrTwoLetterCountryIsoCode}#{civicRegNr}", StoredPersonCollectionName);
        }

        private const string StoredPersonCollectionName = "storedPersons";
        private readonly string baseCountry;
        private readonly bool useFiReformedCenturyMarker;
    }

    public class TestPersonRepository
    {
        private readonly ComposableTestPersonRepository innerRepository;
        private readonly IDocumentDatabase db;

        public TestPersonRepository(string baseCountry, IDocumentDatabase db) : this(baseCountry, db, false)
        {

        }

        public TestPersonRepository(string baseCountry, IDocumentDatabase db, bool useFiReformedCenturyMarker)
        {
            this.innerRepository = new ComposableTestPersonRepository(baseCountry, useFiReformedCenturyMarker);
            this.db = db;
        }

        public StoredPerson GenerateNewTestPerson(bool isAccepted, IRandomnessSource random, DateTime now, bool reuseExisting = false, DateTime? birthDate = null, ICivicRegNumber civicRegNr = null, Dictionary<string, string> overrides = null)
        {
            using (var tr = db.BeginTransaction())
            {
                var result = innerRepository.GenerateNewTestPerson(isAccepted, random, tr, now, reuseExisting: reuseExisting, customBirthDate: birthDate, civicRegNr: civicRegNr, overrides: overrides);
                tr.Commit();
                return result;
            }
        }

        public StoredPerson AddOrUpdate(IDictionary<string, string> properties, bool returnPerson)
        {
            using (var tr = db.BeginTransaction())
            {
                var result = innerRepository.AddOrUpdate(properties, returnPerson, tr);
                tr.Commit();
                return result;
            }
        }

        public void Add(IDictionary<string, string> properties)
        {
            using (var tr = db.BeginTransaction())
            {
                innerRepository.Add(properties, tr);
                tr.Commit();
            }
        }

        public void Update(IDictionary<string, string> properties)
        {
            using (var tr = db.BeginTransaction())
            {
                innerRepository.Update(properties, tr);
                tr.Commit();
            }
        }

        public IDictionary<string, string> Get(string civicRegNrTwoLetterCountryIsoCode, string civicRegNr)
        {
            using (var tr = db.BeginTransaction())
            {
                var result = innerRepository.Get(civicRegNrTwoLetterCountryIsoCode, civicRegNr, tr);
                tr.Commit();
                return result;
            }
        }

        public StoredPerson GetI(string civicRegNrTwoLetterCountryIsoCode, string civicRegNr)
        {
            using (var tr = db.BeginTransaction())
            {
                var result = innerRepository.GetI(civicRegNrTwoLetterCountryIsoCode, civicRegNr, tr);
                tr.Commit();
                return result;
            }
        }
    }

    public class StoredPerson
    {
        public string CivicRegNr { get; set; }
        public string CivicRegNrTwoLetterCountryIsoCode { get; set; }
        public IDictionary<string, string> Properties { get; set; }

        public string GetProperty(string name)
        {
            if (Properties.ContainsKey(name))
                return Properties[name];
            else
                throw new Exception($"Test person {CivicRegNr} is missing {name}");
        }

        public static StoredPerson FromDictionary(IDictionary<string, string> properties)
        {
            var civicRegNr = properties["civicRegNr"];
            var civicRegNrCountry = properties["civicRegNrCountry"];

            return new StoredPerson
            {
                CivicRegNr = civicRegNr,
                CivicRegNrTwoLetterCountryIsoCode = civicRegNrCountry,
                Properties = properties
            };
        }
    }
}