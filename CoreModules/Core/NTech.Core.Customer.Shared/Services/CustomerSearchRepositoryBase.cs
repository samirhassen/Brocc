using nCustomer.DbModel;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCustomer
{
    public abstract class CustomerSearchRepositoryBase : CustomerRepositorySimple
    {
        private readonly ICustomerContext db;

        public CustomerSearchRepositoryBase(
            ICustomerContext db, EncryptionService encryptionService) : base(db, encryptionService)
        {
            this.db = db;
        }

        protected abstract List<string> TranslateSearchTermValue(string term, string value);

        public List<int> FindCustomersMatchingAllSearchTerms(params Tuple<string, string>[] terms)
        {
            if (terms == null || terms.Length == 0)
                return new List<int>();

            var q = db.CustomerSearchTermsQueryable.Where(x => x.IsActive).AsQueryable();

            foreach (var t in terms)
            {
                var translatedValues = TranslateSearchTermValue(t.Item1, t.Item2);
                var termCode = t.Item1.ToString();
                foreach (var translatedValue in translatedValues)
                {
                    q = q.Where(x => db.CustomerSearchTermsQueryable.Any(y => x.CustomerId == y.CustomerId && y.TermCode == termCode && y.IsActive && y.Value == translatedValue));
                }
            }

            return q.Select(x => x.CustomerId).ToList().Distinct().ToList();
        }

        public List<int> FindCustomersByName(string name)
        {
            var nameParts = TranslateSearchTermValue(SearchTermCode.firstName.ToString(), name); //Or last name, doesnt matter since we search by full name

            if (nameParts.Count <= 1)
                return new List<int>(); //Minimum two parts to give a reasonbly small set of hits

            var q = db.CustomerSearchTermsQueryable.Where(x => x.IsActive);
            var termCodes = new List<string>() { SearchTermCode.firstName.ToString(), SearchTermCode.lastName.ToString() };
            return nameParts
                .AsQueryable()
                .SelectMany(namePart =>
                    db
                    .CustomerSearchTermsQueryable
                    .Where(x => termCodes.Contains(x.TermCode) && x.IsActive && x.Value == namePart)
                    .Select(x => x.CustomerId))
                .GroupBy(x => x)
                .Where(x => x.Count() > 1) //At least two name fragments must match
                .Select(x => x.Key)
                .ToList();
        }
    }
}