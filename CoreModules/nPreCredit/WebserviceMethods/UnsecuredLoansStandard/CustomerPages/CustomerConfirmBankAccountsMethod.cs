using nPreCredit.Code.Services;
using nPreCredit.WebserviceMethods.UnsecuredLoansStandard.ApplicationAutomation;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard.CustomerPages
{
    public class CustomerConfirmBankAccountsMethod : TypedWebserviceMethod<CustomerConfirmBankAccountsMethod.Request, CustomerConfirmBankAccountsMethod.Response>
    {
        public override string Path => "UnsecuredLoanStandard/CustomerPages/Confirm-Bank-Accounts";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled;
        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("Middle");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var applicationInfoService = requestContext
                .Resolver()
                .Resolve<ApplicationInfoService>();

            var applicationInfo = applicationInfoService.GetApplicationInfo(request.ApplicationNr, true);

            if (applicationInfo == null)
                return Error("Not found", 400, "noSuchApplication");

            if (!applicationInfo.IsActive)
                return Error("Application is not active", 400, "applicationNotActive");

            if (request.CustomerId.HasValue)
            {
                var applicants = applicationInfoService.GetApplicationApplicants(request.ApplicationNr);
                if (!applicants.CustomerIdByApplicantNr.Values.Contains(request.CustomerId.Value))
                    return Error("No such application exists", 400, "noSuchApplication");
            }

            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                var existingItem = context.ComplexApplicationListItems
                    .SingleOrDefault(item => item.ApplicationNr == request.ApplicationNr
                        && item.ListName == "Application"
                        && item.ItemName == "confirmedBankAccountsCode");

                if (existingItem == null)
                    return Error("Something went wrong, item does not exist on application. ", 500, "internalError");

                if (request.CanToggle.HasValue && request.CanToggle.Value)
                {
                    if (existingItem.ItemValue == "Approved")
                    {
                        existingItem.ItemValue = "Initial";
                        context.CreateAndAddComment("Handler changed confirmed accounts to initial.", "HandlerInitialisedBankAccounts", request.ApplicationNr);
                    }

                    else
                    {
                        existingItem.ItemValue = "Approved";
                        context.CreateAndAddComment("Handler has confirmed bank accounts.", "HandlerConfirmedBankAccounts", request.ApplicationNr);
                    }
                }
                else
                {
                    if (existingItem.ItemValue == "Approved")
                        return Error("Bank accounts has already been confirmed. ", 400, "alreadyConfirmed");

                    existingItem.ItemValue = "Approved";

                    context.CreateAndAddComment($"Customer has updated and confirmed bank accounts. ", "CustomerConfirmedBankAccounts", applicationNr: request.ApplicationNr);
                }

                if (!request.CanToggle.HasValue)
                {
                    var autoHandler = new ApplicationAutomationHandler(applicationInfo,
                        requestContext.Resolver().Resolve<UnsecuredLoanStandardWorkflowService>(),
                        requestContext.Resolver().Resolve<UnsecuredLoanStandardAgreementService>());

                    autoHandler.HandleCustomerFraudAutomation();
                }

                context.SaveChanges();
            }

            return new Response();
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
            public int? CustomerId { get; set; }
            public bool? CanToggle { get; set; }
        }

        public class Response { }

    }
}