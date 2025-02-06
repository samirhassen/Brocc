using Newtonsoft.Json;
using nPreCredit.Code.Services;
using NTech.Banking.BankAccounts;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard.CustomerPages
{

    public class SaveDirectDebitAccountAndOwnerMethod : TypedWebserviceMethod<SaveDirectDebitAccountAndOwnerMethod.Request, SaveDirectDebitAccountAndOwnerMethod.Response>
    {
        public override string Path => "UnsecuredLoanStandard/CustomerPages/Save-DirectDebit-Account";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled;
        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("Middle");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var resolver = requestContext.Resolver();

            var applicationInfoService = resolver.Resolve<ApplicationInfoService>();
            var applicationInfo = applicationInfoService.GetApplicationInfo(request.ApplicationNr, true);

            if (applicationInfo == null)
                return Error("Not found", 400, "noSuchApplication");

            if (!applicationInfo.IsActive)
                return Error("Application is not active", 400, "applicationNotActive");

            var applicants = applicationInfoService.GetApplicationApplicants(request.ApplicationNr);
            if (!applicants.CustomerIdByApplicantNr.Values.Contains(request.CustomerId.Value))
                return Error("No such application exists", 400, "noSuchApplication");

            var listService = resolver.Resolve<IComplexApplicationListService>();

            var directDebitBankAccountNr = new BankAccountNumberParser(NEnv.ClientCfg.Country.BaseCountry).ParseFromStringWithDefaults(request.DirectDebitBankAccountNr, null);

            listService.ChangeList(new List<ComplexApplicationListOperation>
            {
                CreateEditOperation(request.ApplicationNr, "directDebitBankAccountNr", directDebitBankAccountNr?.FormatFor(null)),
                CreateEditOperation(request.ApplicationNr, "directDebitAccountOwnerApplicantNr", request.DirectDebitAccountOwnerApplicantNr.ToString()),
            });

            return new Response();
        }

        private static ComplexApplicationListOperation CreateEditOperation(string applicationNr, string itemName, string newValue) =>
            new ComplexApplicationListOperation
            {
                ApplicationNr = applicationNr,
                Nr = 1,
                ListName = "Application",
                IsDelete = string.IsNullOrWhiteSpace(newValue),
                ItemName = itemName,
                UniqueValue = string.IsNullOrWhiteSpace(newValue) ? null : newValue
            };

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
            [Required]
            public int? CustomerId { get; set; }
            [Required]
            [BankAccountNr]
            public string DirectDebitBankAccountNr { get; set; }
            [Required]
            public int DirectDebitAccountOwnerApplicantNr { get; set; }
        }

        public class Response
        {

        }
    }
}