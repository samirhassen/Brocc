using nCreditReport.Models;
using NTech.Banking.CivicRegNumbers;
using System;
using System.Collections.Generic;

namespace nCreditReport.Code.SatFi
{
    public class SatFiAllZeroTestService : PersonBaseCreditReportService
    {
        public SatFiAllZeroTestService() : base("SatFi")
        {
        }

        public override string ForCountry => "FI";

        protected override Result DoTryBuyCreditReport(ICivicRegNumber civicRegNr, CreditReportRequestData requestData)
        {
            if (NEnv.IsProduction)
                throw new Exception("Test service not allowed in production");
            return new Result
            {
                CreditReport = this.CreateResult(civicRegNr, new List<SaveCreditReportRequest.Item>
                {
                    new SaveCreditReportRequest.Item { Name = "count", Value = "0" },
                    new SaveCreditReportRequest.Item { Name = "c01", Value = "0" },
                    new SaveCreditReportRequest.Item { Name = "c03", Value = "0" },
                    new SaveCreditReportRequest.Item { Name = "c04", Value = "0" }
                }, requestData),
                IsError = false
            };
        }
    }
}