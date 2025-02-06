using Dapper;
using Microsoft.EntityFrameworkCore;
using nCustomer;
using nCustomer.DbModel;
using NTech.Banking.CivicRegNumbers;
using NTech.Banking.OrganisationNumbers;
using NTech.Core.Customer.Database;
using NTech.Core.Customer.Shared.Helpers;
using NTech.Core.Customer.Shared.Services;
using NTech.Core.Module.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;

namespace NTech.Core.Customer.Services
{
    public class CustomerCreationService : CustomerServiceBase
    {
        private readonly ICoreClock clock;
        private readonly INTechCurrentUserMetadata currentUser;
        private readonly IClientConfigurationCore clientConfiguration;

        public CustomerCreationService(ICoreClock clock, INTechCurrentUserMetadata currentUser, IClientConfigurationCore clientConfiguration)
        {
            this.clock = clock;
            this.currentUser = currentUser;
            this.clientConfiguration = clientConfiguration;
        }

        public int CreateNewPerson(CustomerContext context, ICivicRegNumber civicRegNr, IDictionary<string, string> properties, int? expectedCustomerId = null, string externalEventCode = null)
        {
            CheckForBannedProperties(properties);

            var actualCustomerId = GetCustomerIdByCivicRegNr(civicRegNr);
            if (expectedCustomerId.HasValue && expectedCustomerId.Value != actualCustomerId)
                throw CreateWebserviceException($"Expected customerid to be {expectedCustomerId.Value} but it was instead {actualCustomerId}");

            if (context.CustomerProperties.Any(x => x.CustomerId == actualCustomerId))
                throw CreateWebserviceException("Customer already exists");

            var items = new List<CustomerPropertyModel>();

            void Add(string name, string value) =>
                items.Add(CustomerPropertyModel.Create(actualCustomerId, name, value, false));

            Add(CustomerProperty.Codes.civicRegNr.ToString(), civicRegNr.NormalizedValue);

            foreach (var p in properties)
                Add(p.Key, p.Value);

            if (!items.Any(x => x.Name == CustomerProperty.Codes.birthDate.ToString()) && civicRegNr.BirthDate.HasValue)
                Add(CustomerProperty.Codes.birthDate.ToString(), civicRegNr.BirthDate.Value.ToString("yyyy-MM-dd"));

            if (items.Any(x => CustomerPropertyModel.AdressHashFieldNames.Contains(x.Name)))
            {
                var addressHash = ComputeAddressHash(items);
                Add(CustomerProperty.Codes.addressHash.ToString(), addressHash);
            }

            foreach (var item in items)
            {
                context.CustomerProperties.Add(new CustomerProperty
                {
                    CustomerId = actualCustomerId,
                    Value = item.Value,
                    Name = item.Name,
                    Group = item.Group,
                    IsCurrentData = true,
                    IsEncrypted = false,
                    CreatedByEvent = null,
                    IsSensitive = item.IsSensitive
                }.PopulateInfraFields(currentUser, clock));
            }

            PopulateSearchTermsForNewCustomer(actualCustomerId, items, context);

            return actualCustomerId;
        }

        private void PopulateSearchTermsForNewCustomer(int customerId, List<CustomerPropertyModel> items, CustomerContext context)
        {
            void AddSearchTerms(string termCode, List<string> values) => context.CustomerSearchTerms.AddRange(
                values.Select(value => new CustomerSearchTerm
                {
                    CustomerId = customerId,
                    IsActive = true,
                    TermCode = termCode,
                    Value = value
                }.PopulateInfraFields(currentUser, clock)));

            foreach (var item in items)
            {
                if (item.Name == CustomerProperty.Codes.email.ToString())
                {
                    AddSearchTerms("email", CustomerSearchTermHelper.ComputeEmailSearchTerms(item.Value));
                }
                else if (item.Name == CustomerProperty.Codes.firstName.ToString() || item.Name == CustomerProperty.Codes.lastName.ToString())
                {
                    AddSearchTerms(item.Name, CustomerSearchTermHelper.ComputeNameSearchTerms(item.Value));
                }
                else if (item.Name == CustomerProperty.Codes.phone.ToString())
                {
                    AddSearchTerms("phone", CustomerSearchTermHelper.ComputePhoneNrSearchTerms(item.Value, clientConfiguration.Country.BaseCountry));
                }
                else if (item.Name == CustomerProperty.Codes.companyName.ToString())
                {
                    // Needs to be ported from or shared with legacy
                    throw new NotImplementedException();
                }
            }
        }

        public int GetCustomerIdByCivicRegNr(ICivicRegNumber civicRegNr, CustomerContext context = null)
        {
            if (civicRegNr == null)
                throw new ArgumentNullException("civicRegNr");
            return GetCustomerIdByNr(civicRegNr.NormalizedValue, context: context);
        }

        public int GetCustomerIdByOrgnr(IOrganisationNumber orgnr, CustomerContext context = null)
        {
            if (orgnr == null)
                throw new ArgumentNullException("orgnr");
            //We use a prefix to allow the things like swedish enskild firma to exist as both a private person with civicregnr and a company with the same orgnr but different customer ids
            return GetCustomerIdByNr($"C" + orgnr.NormalizedValue, context: context);
        }

        private static int GetCustomerIdByNr(string nr, CustomerContext context = null)
        {
            if (nr == null)
                throw new ArgumentNullException("nr");

            int GetCustomerIdWithContext(CustomerContext c)
            {
                var hash = ComputeCustomerCivicOrOrgnrToCustomerIdMappingHash(nr);
                var customerDbConnection = c.Database.GetDbConnection();
                return customerDbConnection.ExecuteScalar<int>(CreateCustomerIdSql, new { hash });
            };

            if (context != null)
                return GetCustomerIdWithContext(context);
            else
                using (var ctx = new CustomerContext())
                {
                    return GetCustomerIdWithContext(ctx);
                }
        }
    }
}
