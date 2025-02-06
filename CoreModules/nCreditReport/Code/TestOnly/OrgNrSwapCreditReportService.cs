using nCreditReport.Models;
using NTech.Banking.OrganisationNumbers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCreditReport.Code
{
    public class OrgNrSwapCreditReportService : CompanyBaseCreditReportService
    {
        public override string ForCountry => realProvider.ForCountry;

        private readonly CompanyBaseCreditReportService realProvider;
        private readonly Func<IOrganisationNumber, Tuple<IOrganisationNumber, Dictionary<string, string>>> getSubstitutionOrgnr;

        public OrgNrSwapCreditReportService(CompanyBaseCreditReportService realProvider, Func<IOrganisationNumber, Tuple<IOrganisationNumber, Dictionary<string, string>>> getSubstitutionOrgnr) : base(realProvider.ProviderName)
        {
            this.realProvider = realProvider;
            this.getSubstitutionOrgnr = getSubstitutionOrgnr;
        }

        protected override Result DoTryBuyCreditReport(IOrganisationNumber orgnr, CreditReportRequestData requestData)
        {
            if (NEnv.IsProduction)
                throw new Exception("Not allowed in production");

            var subOrgnrAndReplacementItems = getSubstitutionOrgnr(orgnr);
            var subOrgnr = subOrgnrAndReplacementItems.Item1;
            var replacementItems = subOrgnrAndReplacementItems.Item2;

            var result = realProvider.TryBuyCreditReport(subOrgnr, requestData);

            var r = new Result
            {
                ErrorMessage = result.ErrorMessage,
                IsError = result.IsError,
                IsInvalidCredentialsError = result.IsInvalidCredentialsError,
                IsTimeoutError = result.IsTimeoutError
            };
            var c = result.CreditReport;
            if (c != null)
            {
                r.CreditReport = new SaveCreditReportRequest
                {
                    ChangedById = c.ChangedById,
                    CreationDate = c.CreationDate,
                    CreditReportProviderName = realProvider.ProviderName,
                    InformationMetaData = c.InformationMetaData,
                    RequestDate = c.RequestDate,
                    SearchTerms = HandleSearchTerms(orgnr, realProvider.ProviderName, c.SearchTerms),
                    Items = HandleItems(orgnr, c.Items, replacementItems)
                };
            }

            return r;
        }

        private List<SaveCreditReportRequest.Item> HandleSearchTerms(IOrganisationNumber originalOrgnr, string originalProviderName, List<SaveCreditReportRequest.Item> items)
        {
            return items.Select(x =>
            {
                if (x.Name == "providerName")
                    return new SaveCreditReportRequest.Item { Name = x.Name, Value = originalProviderName };
                else
                    return x;
            }).ToList();
        }

        private List<SaveCreditReportRequest.Item> HandleItems(IOrganisationNumber originalOrgnr, List<SaveCreditReportRequest.Item> items, Dictionary<string, string> replacementItems)
        {
            return items.Select(x =>
            {
                if (x.Name == "orgnr")
                    return new SaveCreditReportRequest.Item { Name = x.Name, Value = originalOrgnr.NormalizedValue };
                else if (replacementItems.ContainsKey(x.Name))
                    return new SaveCreditReportRequest.Item { Name = x.Name, Value = replacementItems[x.Name] };
                else
                    return x;
            }).ToList();
        }
    }
}