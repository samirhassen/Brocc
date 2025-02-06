using nCreditReport.Models;
using NTech;
using NTech.Banking.CivicRegNumbers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCreditReport.Code
{
    public abstract class PersonBaseCreditReportService : BaseCreditReportService
    {
        public PersonBaseCreditReportService(string providerName) : base(providerName)
        {

        }

        public override bool IsCompanyProvider => false;

        public Result TryBuyCreditReport(
            ICivicRegNumber civicRegNr,
            CreditReportRequestData requestData)
        {
            if (civicRegNr == null)
                throw new ArgumentNullException("civicRegNr");

            if (ForCountry != civicRegNr.Country)
                throw new Exception($"Provider only supports persons from {ForCountry}");

            return DoTryBuyCreditReport(civicRegNr, requestData);
        }

        protected abstract Result DoTryBuyCreditReport(ICivicRegNumber civicRegNr, CreditReportRequestData requestData);

        protected SaveCreditReportRequest CreateResult(ICivicRegNumber civicRegNr, IEnumerable<SaveCreditReportRequest.Item> items, CreditReportRequestData requestData)
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
                            Value = civicRegNr.Country
                        },
                        new SaveCreditReportRequest.Item
                        {
                            Name = "birthDate",
                            Value = civicRegNr.BirthDate.Value.ToString("yyyy-MM-dd")
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
                    }.Where(x => x != null).ToList(),
                Items = items
                    .Where(x => x.Name != "civicRegNr")
                    .Concat(
                        new List<SaveCreditReportRequest.Item>()
                        {
                            new SaveCreditReportRequest.Item
                            {
                                Name = "civicRegNr",
                                Value = civicRegNr.NormalizedValue
                            }
                        }).ToList()
            };
        }
    }
}