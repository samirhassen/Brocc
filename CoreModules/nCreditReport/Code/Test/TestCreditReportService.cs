using nCreditReport.Models;
using NTech.Banking.CivicRegNumbers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCreditReport.Code
{
    public class TestCreditReportService : PersonBaseCreditReportService
    {
        protected PersonBaseCreditReportService actualService;
        private string forCountry;
        private Lazy<IDictionary<string, List<Tuple<string, string>>>> testPersons = new Lazy<IDictionary<string, List<Tuple<string, string>>>>(TestPersonsFactory.Create);

        public TestCreditReportService(PersonBaseCreditReportService actualService) : base(actualService.ProviderName)
        {
            this.actualService = actualService;
            this.forCountry = actualService.ForCountry;
        }

        public TestCreditReportService(string providerName, string forCountry) : base(providerName)
        {
            this.actualService = null;
            this.forCountry = forCountry;
        }

        public bool DoesTestPersonExist(ICivicRegNumber civicRegNr)
        {
            return testPersons.Value.ContainsKey(civicRegNr.NormalizedValue);
        }

        public override string ForCountry
        {
            get
            {
                return forCountry;
            }
        }

        protected override Result DoTryBuyCreditReport(ICivicRegNumber civicRegNr, CreditReportRequestData requestData)
        {
            if (DoesTestPersonExist(civicRegNr))
            {
                var fields = testPersons.Value[civicRegNr.NormalizedValue];
                var result = CreateResult(civicRegNr, fields
                    .Select(x => new SaveCreditReportRequest.Item
                    {
                        Name = x.Item1,
                        Value = x.Item2
                    }), requestData);
                return new Result
                {
                    CreditReport = result,
                    ErrorMessage = null,
                    IsError = false
                };
            }
            else if (actualService != null)
            {
                return actualService.TryBuyCreditReport(civicRegNr, requestData);
            }
            else
            {
                return new Result
                {
                    IsError = false,
                    IsInvalidCredentialsError = false,
                    ErrorMessage = null,
                    CreditReport = CreateResult(civicRegNr, new List<SaveCreditReportRequest.Item>
                        {
                            new SaveCreditReportRequest.Item
                            {
                                Name = "personStatus",
                                Value = "nodata"
                            }
                        }, requestData)
                };
            }
        }
    }
}