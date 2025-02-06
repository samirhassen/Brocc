using NTech.Banking.BankAccounts;
using NTech.Banking.OrganisationNumbers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace nTest.RandomDataSource
{
    public class ComposableTestCompanyRepository
    {

        public ComposableTestCompanyRepository(string baseCountry, Func<Stream, string> storeHtmlDocumentInArchive)
        {
            this.baseCountry = baseCountry;
            this.storeHtmlDocumentInArchive = storeHtmlDocumentInArchive;
        }

        private const string OrgnrCode = "orgnr";
        private const string OrgnrCountryCode = "orgnrCountry";

        //reuseExisting: This is a good idea when the same random seed is reused. Eventually the 10 tries will be hit and it will stop working otherwise
        public StoredCompany GenerateNewTestCompany(bool isAccepted, IRandomnessSource random, IDocumentDatabaseUnitOfWork tr, DateTime now, bool? reuseExisting = false, DateTime? customBirthDate = null, IOrganisationNumber orgnr = null, Dictionary<string, string> overrides = null, BankAccountNumberTypeCode? bankAccountNrType = null)
        {
            Func<StoredCompany> gen = () =>
            {
                var g = new OrganisationNumberGenerator(this.baseCountry);
                var p = new OrganisationNumberParser(this.baseCountry);

                IOrganisationNumber newOrgnr;

                if (orgnr != null)
                {
                    reuseExisting = true;
                    newOrgnr = orgnr;
                }
                else
                {
                    var r = new Random(random.NextIntBetween(0, 10000000));
                    newOrgnr = g.Generate(r.Next);
                }

                Func<StoredCompany, bool> applyOverrides = pp =>
                {
                    if (overrides == null || overrides.Count == 0)
                        return false;
                    foreach (var v in overrides)
                        pp.Properties[v.Key] = v.Value;
                    return true;
                };

                var existingCustomer = GetI(newOrgnr.Country, newOrgnr.NormalizedValue, tr);
                if (existingCustomer != null)
                {
                    if (reuseExisting.GetValueOrDefault())
                    {
                        if (applyOverrides(existingCustomer))
                        {
                            tr.AddOrUpdate($"{existingCustomer.OrgnrNrTwoLetterCountryIsoCode}#{existingCustomer.Orgnr}", StoredCompanyCollectionName, existingCustomer);
                        }
                        return existingCustomer;
                    }
                    else
                        return null;
                }

                var newProps = TestCompanyGenerator.GetSharedInstance(this.baseCountry).GenerateTestCompany(random, newOrgnr, isAccepted, now, new Random(random.NextIntBetween(0, 10000000)), bankAccountNrType ?? BankAccountNumberParser.GetDefaultAccountTypeByCountryCode(baseCountry), this.storeHtmlDocumentInArchive);

                var company = new StoredCompany
                {
                    Orgnr = newOrgnr.NormalizedValue,
                    OrgnrNrTwoLetterCountryIsoCode = newOrgnr.Country,
                    Properties = newProps
                };
                applyOverrides(company);

                tr.AddOrUpdate($"{company.OrgnrNrTwoLetterCountryIsoCode}#{company.Orgnr}", StoredCompanyCollectionName, company);

                return company;
            };

            for (var n = 0; n < 10; ++n)
            {
                var newPerson = gen();
                if (newPerson != null)
                    return newPerson;
            }

            throw new Exception("Failed to generate testcompany after 10 tries");
        }

        public StoredCompany AddOrUpdate(IDictionary<string, string> properties, bool returnCompany, IDocumentDatabaseUnitOfWork tr)
        {
            var orgnr = properties[OrgnrCode];
            var orgnrTwoLetterCountryIsoCode = properties[OrgnrCountryCode];
            if (GetI(orgnrTwoLetterCountryIsoCode, orgnr, tr) != null)
                Update(properties, tr);
            else
                Add(properties, tr);
            if (returnCompany)
                return GetI(orgnrTwoLetterCountryIsoCode, orgnr, tr);
            else
                return null;
        }

        public void Add(IDictionary<string, string> properties, IDocumentDatabaseUnitOfWork tr)
        {
            var company = new StoredCompany
            {
                Orgnr = properties[OrgnrCode],
                OrgnrNrTwoLetterCountryIsoCode = properties[OrgnrCountryCode],
                Properties = properties
                    .Where(x => x.Key != OrgnrCountryCode && x.Key != OrgnrCode)
                    .ToDictionary(x => x.Key.ToString(), x => x.Value)
            };
            tr.AddOrUpdate($"{company.OrgnrNrTwoLetterCountryIsoCode}#{company.Orgnr}", StoredCompanyCollectionName, company);
        }

        public void Update(IDictionary<string, string> properties, IDocumentDatabaseUnitOfWork tr)
        {
            var orgnr = properties[OrgnrCode];
            var orgnrTwoLetterCountryIsoCode = properties[OrgnrCountryCode];
            var result = GetI(orgnrTwoLetterCountryIsoCode, orgnr, tr);
            if (result == null)
                throw new Exception("No such company!");
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
                    .Where(x => x.Key != OrgnrCountryCode && x.Key != OrgnrCode)
                    .ToDictionary(x => x.Key.ToString(), x => x.Value);

            tr.AddOrUpdate($"{result.OrgnrNrTwoLetterCountryIsoCode}#{result.Orgnr}", StoredCompanyCollectionName, result);
        }

        public IDictionary<string, string> Get(string orgnrTwoLetterCountryIsoCode, string orgnr, IDocumentDatabaseUnitOfWork tr)
        {
            var result = GetI(orgnrTwoLetterCountryIsoCode, orgnr, tr);
            if (result == null)
                return null;

            var d = new Dictionary<string, string>();
            d[OrgnrCode] = result.Orgnr;
            d[OrgnrCountryCode] = result.OrgnrNrTwoLetterCountryIsoCode;
            return result.Properties;
        }

        public StoredCompany GetI(string orgnrCountryIsoCode, string orgnr, IDocumentDatabaseUnitOfWork tr)
        {
            return tr.Get<StoredCompany>($"{orgnrCountryIsoCode}#{orgnr}", StoredCompanyCollectionName);
        }

        private const string StoredCompanyCollectionName = "storedCompanies";
        private readonly string baseCountry;
        private readonly Func<Stream, string> storeHtmlDocumentInArchive;
    }

    public class TestCompanyRepository
    {
        private readonly ComposableTestCompanyRepository innerRepository;
        private readonly IDocumentDatabase db;

        public TestCompanyRepository(string baseCountry, IDocumentDatabase db, Func<Stream, string> storeHtmlDocumentInArchive)
        {
            this.innerRepository = new ComposableTestCompanyRepository(baseCountry, storeHtmlDocumentInArchive);
            this.db = db;
        }

        public StoredCompany GenerateNewTestCompany(bool isAccepted, IRandomnessSource random, DateTime now, bool reuseExisting = false, IOrganisationNumber orgnr = null, Dictionary<string, string> overrides = null, BankAccountNumberTypeCode? bankAccountNrType = null)
        {
            using (var tr = db.BeginTransaction())
            {
                var result = innerRepository.GenerateNewTestCompany(isAccepted, random, tr, now, reuseExisting: reuseExisting, orgnr: orgnr, overrides: overrides, bankAccountNrType: bankAccountNrType);
                tr.Commit();
                return result;
            }
        }

        public StoredCompany AddOrUpdate(IDictionary<string, string> properties, bool returnPerson)
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

        public IDictionary<string, string> Get(string orgnrTwoLetterCountryIsoCode, string orgnr)
        {
            using (var tr = db.BeginTransaction())
            {
                var result = innerRepository.Get(orgnrTwoLetterCountryIsoCode, orgnr, tr);
                tr.Commit();
                return result;
            }
        }

        public StoredCompany GetI(string orgnrTwoLetterCountryIsoCode, string orgnr)
        {
            using (var tr = db.BeginTransaction())
            {
                var result = innerRepository.GetI(orgnrTwoLetterCountryIsoCode, orgnr, tr);
                tr.Commit();
                return result;
            }
        }
    }

    public class StoredCompany
    {
        public string Orgnr { get; set; }
        public string OrgnrNrTwoLetterCountryIsoCode { get; set; }
        public IDictionary<string, string> Properties { get; set; }

        public string GetProperty(string name)
        {
            if (Properties.ContainsKey(name))
                return Properties[name];
            else
                throw new Exception($"Test company {Orgnr} is missing {name}");
        }

        public static StoredCompany FromDictionary(IDictionary<string, string> properties)
        {
            return new StoredCompany
            {
                Orgnr = properties["orgnr"],
                OrgnrNrTwoLetterCountryIsoCode = properties["orgnrCountry"],
                Properties = properties
            };
        }
    }
}