using nCreditReport.Models;
using NTech.Banking.OrganisationNumbers;
using System;
using System.Linq;

namespace nCreditReport.Code.UcSe
{
    public class TestAwareUcBusinessSeService : UcBusinessSeService
    {
        public TestAwareUcBusinessSeService(string providerName) : base(providerName)
        {
        }

        public TestAwareUcBusinessSeService(string providerName, NEnv.UcBusinessSeSettings settings) : base(providerName, settings)
        {
        }

        public TestAwareUcBusinessSeService(NEnv.UcBusinessSeSettings settings, string logFolder, string providerName, IDocumentClient documentClient) : base(settings, logFolder, providerName, documentClient)
        {
        }

        protected override Result DoTryBuyCreditReport(IOrganisationNumber orgnr, CreditReportRequestData requestData)
        {
            Func<bool, Result> getFromTestModule = (generateIfNotExists) =>
            {
                var tc = new nTestClient();

                var tp = tc.GetTestCompany(orgnr, generateIfNotExists: generateIfNotExists);
                if (tp == null)
                    return null;
                else
                {
                    var items = tp.Where(x => x.Key.StartsWith("creditreport_")).Select(x => new SaveCreditReportRequest.Item
                    {
                        Name = x.Key.Substring("creditreport_".Length),
                        Value = x.Value
                    }).ToList();

                    return new Result
                    {
                        CreditReport = this.CreateResult(orgnr, items, requestData)
                    };
                }
            };

            if ((this.settings.TestModuleMode ?? "").IsOneOfIgnoreCase("only"))
                return getFromTestModule(true);

            if ((this.settings.TestModuleMode ?? "").IsOneOfIgnoreCase("preferred"))
            {
                var testModuleResult = getFromTestModule(false);
                if (testModuleResult != null)
                    return testModuleResult;
            }

            var result = base.DoTryBuyCreditReport(orgnr, requestData);

            if ((this.settings.TestModuleMode ?? "").IsOneOfIgnoreCase("fallback") && result.IsError && (result.ErrorMessage ?? "").Contains("Objekt-nr saknas i UC:s register"))
                return getFromTestModule(true);
            else
                return result;
        }
    }
}