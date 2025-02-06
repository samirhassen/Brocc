using nCreditReport.Models;
using NTech;
using NTech.Banking.OrganisationNumbers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCreditReport.Code
{
    public abstract class CompanyBaseCreditReportService : BaseCreditReportService
    {
        public CompanyBaseCreditReportService(string providerName) : base(providerName)
        {

        }

        public override bool IsCompanyProvider => true;

        public Result TryBuyCreditReport(
            IOrganisationNumber orgnr,
            CreditReportRequestData requestData)
        {
            if (orgnr == null)
                throw new ArgumentNullException("orgnr");

            if (ForCountry != orgnr.Country)
                throw new Exception($"Provider only supports persons from {ForCountry}");

            return DoTryBuyCreditReport(orgnr, requestData);
        }

        protected abstract Result DoTryBuyCreditReport(
            IOrganisationNumber orgnr,
            CreditReportRequestData requestData);

        protected SaveCreditReportRequest CreateResult(IOrganisationNumber orgnr, IEnumerable<SaveCreditReportRequest.Item> items, CreditReportRequestData requestData)
        {
            var now = ClockFactory.SharedInstance.Now;
            return new SaveCreditReportRequest
            {
                ChangedById = requestData.UserId,
                CreationDate = now,
                CreditReportProviderName = ProviderName,
                RequestDate = now,
                InformationMetaData = requestData.InformationMetadata,
                SearchTerms = new List<SaveCreditReportRequest.Item>()
                    {
                        new SaveCreditReportRequest.Item
                        {
                            Name = "country",
                            Value = orgnr.Country
                        },
                        new SaveCreditReportRequest.Item
                        {
                            Name = "providerName",
                            Value = ProviderName
                        },
                        string.IsNullOrWhiteSpace(requestData.ReasonType) ? null : new SaveCreditReportRequest.Item
                        {
                            Name = "reasonType",
                            Value = requestData.ReasonType
                        },
                        string.IsNullOrWhiteSpace(requestData.ReasonData) ? null : new SaveCreditReportRequest.Item
                        {
                            Name = "reasonData",
                            Value = requestData.ReasonData
                        }
                    },
                Items = items
                    .Where(x => x.Name != "orgnr")
                    .Concat(
                        new List<SaveCreditReportRequest.Item>()
                        {
                            new SaveCreditReportRequest.Item
                            {
                                Name = "orgnr",
                                Value = orgnr.NormalizedValue
                            }
                        }).ToList()
            };
        }
    }
}