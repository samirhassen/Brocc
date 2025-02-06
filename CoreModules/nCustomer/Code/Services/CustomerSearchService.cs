using nCustomer.DbModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCustomer.Code.Services
{
    public class CustomerSearchService
    {
        private readonly Func<CustomersContext, CustomerSearchRepository> createCustomerRepository;
        private readonly Func<ICompanyLoanNameSearchService> createCompanySearchService;

        public CustomerSearchService(Func<CustomersContext, CustomerSearchRepository> createCustomerRepository, Func<ICompanyLoanNameSearchService> createCompanySearchService)
        {
            this.createCustomerRepository = createCustomerRepository;
            this.createCompanySearchService = createCompanySearchService;
        }

        public ISet<int> FindCustomersByOmniQuery(string searchQuery)
        {
            using (var context = new CustomersContext())
            {
                var customerRepository = createCustomerRepository(context);
                return FindCustomersByOmniQuery(searchQuery, customerRepository, context);
            }
        }

        private ISet<int> FindCustomersByOmniQuery(string searchQuery, CustomerSearchRepository customerRepository, CustomersContext context)
        {
            searchQuery = searchQuery.NormalizeNullOrWhitespace();

            if (searchQuery == null)
                return Enumerable.Empty<int>().ToHashSet();

            var customerIds = new HashSet<int>();

            var exactMatchQuery = GetExactMatchSearchTermOrNull(searchQuery);
            var searchTerms = new List<Tuple<string, string>>();
            if (searchQuery.Contains("@")
                && exactMatchQuery == null //To allow searching for exact company names with @ in using " .... "
                && !searchQuery.Contains(' ') //To allow full text search for company names with @ in them. This assumes valid emails cannot contain whitespace
                )
            {
                customerIds.AddRange(customerRepository.FindCustomersMatchingAllSearchTerms(Tuple.Create("email", searchQuery)));
            }

            if (searchQuery.Any(Char.IsDigit) && !searchQuery.Any(Char.IsLetter))
            {
                customerIds.AddRange(customerRepository.FindCustomersMatchingAllSearchTerms(Tuple.Create("phone", searchQuery)));
            }

            if (NEnv.BaseCivicRegNumberParser.TryParse(searchQuery, out var civicRegNr))
            {
                var customerId = CustomerIdSource.GetCustomerIdByCivicRegNr(civicRegNr, context: context);
                if (context.CustomerProperties.Any(y => y.CustomerId == customerId))
                    customerIds.Add(customerId);
            }

            if (NEnv.IsCompanyLoansEnabled && NEnv.BaseOrganisationNumberParser.TryParse(searchQuery, out var orgNr))
            {
                var customerId = CustomerIdSource.GetCustomerIdByOrgnr(orgNr, context: context);
                if (context.CustomerProperties.Any(y => y.CustomerId == customerId))
                    customerIds.Add(customerId);
            }

            customerIds.AddRange(FindCustomersByName(searchQuery, customerRepository, context));

            return customerIds;
        }

        private ISet<int> FindCustomersByName(string searchQuery, CustomerSearchRepository customerRepository, CustomersContext context)
        {
            var exactMatchQuery = GetExactMatchSearchTermOrNull(searchQuery);
            var customerIds = new HashSet<int>();

            //Company
            if (NEnv.IsCompanyLoansEnabled)
            {
                var companySearchService = createCompanySearchService();
                if (exactMatchQuery != null)
                {
                    customerIds.AddRange(customerRepository.FindCustomersByExactCompanyName(exactMatchQuery));
                }
                else if (LooksLikeACompanyName(searchQuery))
                {
                    customerIds.AddRange(companySearchService.FindCustomerByCompanyName(searchQuery));
                }
            }

            //Person
            if (exactMatchQuery != null)
            {
                customerIds.AddRange(customerRepository.FindCustomersByExactFirstName(exactMatchQuery).ToHashSet());
            }
            else if (LooksLikeAPersonName(searchQuery))
            {
                customerIds.AddRange(customerRepository.FindCustomersByName(searchQuery).ToHashSet());
            }

            return customerIds;
        }

        public static string GetExactMatchSearchTermOrNull(string value)
        {
            var v = value.NormalizeNullOrWhitespace();

            if (v.Length >= 3 && v.StartsWith("\"") && v.EndsWith("\""))
                return v.Substring(1, v.Length - 2);
            else
                return null;
        }

        private static bool LooksLikeAPersonName(string value)
        {
            if (value == null)
                return false;

            //At least two words with no digits
            return value.Split(' ').Length > 1 && !value.Any(char.IsDigit);
        }

        private static bool LooksLikeACompanyName(string value)
        {
            if (value == null)
                return false;

            //At least two words
            return value.Split(' ').Length > 1;
        }
    }
}