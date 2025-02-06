using NTech.Banking.CivicRegNumbers;
using NTech.Banking.OrganisationNumbers;
using NTech.Core.Module.Shared.Clients;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services.CompanyLoans
{
    public class CompanyLoanCustomerCardUpdateService : ICompanyLoanCustomerCardUpdateService
    {
        private readonly ICustomerClient customerClient;

        public CompanyLoanCustomerCardUpdateService(ICustomerClient customerClient)
        {
            this.customerClient = customerClient;
        }

        public int CreateOrUpdateCompany(IOrganisationNumber orgnr, Dictionary<string, string> customerData, bool isTrustedSource, CompanyLoanCustomerRoleCode roleCode, CompanyLoanCustomerCardUpdateEventCode eventCode, string eventSourceId, int? expectedCustomerId = null)
        {
            return this.customerClient.CreateOrUpdateCompany(new CreateOrUpdateCompanyRequest
            {
                CompanyName = customerData.Opt("companyName"),
                EventSourceId = eventSourceId,
                EventType = eventCode.ToString(),
                ExpectedCustomerId = expectedCustomerId,
                Orgnr = orgnr.NormalizedValue,
                Properties = customerData.Select(x => new CreateOrUpdateCompanyRequest.Property
                {
                    Name = x.Key,
                    Value = x.Value,
                    ForceUpdate = isTrustedSource
                }).ToList()
            });
        }

        public int CreateOrUpdatePerson(ICivicRegNumber civicRegNr, Dictionary<string, string> customerData, bool isTrustedSource, CompanyLoanCustomerRoleCode roleCode, CompanyLoanCustomerCardUpdateEventCode eventCode, string eventSourceId, int? expectedCustomerId = null, DateTime? birthDate = null)
        {
            return this.customerClient.CreateOrUpdatePerson(new CreateOrUpdatePersonRequest
            {
                EventSourceId = eventSourceId,
                EventType = eventCode.ToString(),
                ExpectedCustomerId = expectedCustomerId,
                CivicRegNr = civicRegNr.NormalizedValue,
                BirthDate = birthDate,
                Properties = customerData.Select(x => new CreateOrUpdatePersonRequest.Property
                {
                    Name = x.Key,
                    Value = x.Value,
                    ForceUpdate = isTrustedSource
                }).ToList()
            });
        }
    }

    public enum CompanyLoanCustomerCardUpdateEventCode
    {
        NewCreditReport,
        NewApplication
    }

    public enum CompanyLoanCustomerRoleCode
    {
        Applicant,
        CustomerCompany,
        Signatory, //Allowed to sign agreement on the companys behalf. Can be firmatecknare or basically anyone who the board has authorized to do so. All of these + the Guarantors must sign the initial agreement for instance
        Guarantor //Borgensman
    }

    public interface ICompanyLoanCustomerCardUpdateService
    {
        int CreateOrUpdateCompany(IOrganisationNumber orgnr, Dictionary<string, string> customerData, bool isTrustedSource, CompanyLoanCustomerRoleCode roleCode, CompanyLoanCustomerCardUpdateEventCode eventCode, string eventSourceId, int? expectedCustomerId = null);
        int CreateOrUpdatePerson(ICivicRegNumber civicRegNr, Dictionary<string, string> customerData, bool isTrustedSource, CompanyLoanCustomerRoleCode roleCode, CompanyLoanCustomerCardUpdateEventCode eventCode, string eventSourceId, int? expectedCustomerId = null, DateTime? birthDate = null);
    }
}