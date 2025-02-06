using NTech.Banking.CivicRegNumbers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCreditReport.Code.SatFi
{
    public class TestModuleSatFiService : PersonBaseCreditReportService
    {
        public override string ForCountry => "FI";

        public TestModuleSatFiService() : base("SatFi")
        {

        }

        protected override Result DoTryBuyCreditReport(ICivicRegNumber civicRegNr, CreditReportRequestData requestData)
        {
            if (civicRegNr.Country != ForCountry)
                throw new Exception("SatFi can only score finnish persons");

            if (NEnv.IsProduction)
                throw new Exception("The test provider is not allowed in production. Did you forget to set the ntech.satfi.userid, ntech.satfi.password and ntech.satfi.hashkey settings?");

            var c = new nTestClient();

            var itemsToFetch = new List<string>
                {
                    "count", "c15", "h14", "d11", "d12", "e11", "e12", "f11", "f12", "f13", "h15", "h16", "k11", "k12", "c01", "c02", "c03", "c04",
                    "test_istimeout"
                }.Select(x => $"satfi_{x}").ToList();
            var result = c
                .GetTestPerson("satfi_", civicRegNr, itemsToFetch.ToArray())
                ?.ToDictionary(x => x.Key.Substring("satfi_".Length), x => x.Value);

            if (result == null)
                result = new Dictionary<string, string>();

            bool isTimeout = false;
            if (result.ContainsKey("test_istimeout"))
            {
                isTimeout = result["test_istimeout"]?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
                result.Remove("test_istimeout");
            }

            if (isTimeout)
            {
                return new Result
                {
                    IsError = true,
                    IsTimeoutError = true,
                    ErrorMessage = "Obscure technobabble errormessage (test)"
                };
            }

            var satResponse = new SatFiService.SatConsumerLoanSummaryResponse
            {
                CountLoans = result.ContainsKey("count") ? new int?(int.Parse(result["count"])) : null,
                ResponseStatus = SatFiService.ResponseStatusCode.Ok,
                Rows = result.Where(x => x.Key != "count").ToDictionary(x => x.Key, x => new SatFiService.ConsumerLoanRow
                {
                    Code = x.Key,
                    Value = x.Value,
                    Text = $"Testitem {x.Key}"
                })
            };

            var items = SatFiService.TranslateSatResponse(satResponse);
            return new Result
            {
                CreditReport = this.CreateResult(civicRegNr, items, requestData),
                IsError = false
            };
        }
    }
}