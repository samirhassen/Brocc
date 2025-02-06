using nPreCredit.Code;
using nPreCredit.Code.Services;
using NTech.Core.PreCredit.Shared;
using NTech.Core.PreCredit.Shared.Services.UlStandard;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class UnsecuredLoanStandardCreateLoanMethod : TypedWebserviceMethod<CreateLoanFromApplicationUlStandardRequest, CreateLoanFromApplicationUlStandardResponse>
    {
        public override string Path => "UnsecuredLoanStandard/Create-Loan";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled;
        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("Middle");

        protected override CreateLoanFromApplicationUlStandardResponse DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, CreateLoanFromApplicationUlStandardRequest request)
        {
            ValidateUsingAnnotations(request);

            var service = requestContext.Resolver().Resolve<CreateLoanFromApplicationUlStandardService>();
            return service.CreateLoan(request);
        }

        public static (bool WasCreated, string CreditNr) EnsureCreditNr(ApplicationInfoModel ai, ComplexApplicationList.Row applicationRow, IPreCreditContextExtended context)
        {
            var creditClient = LegacyServiceClientFactory.CreateCreditClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry);
            return CreateLoanFromApplicationUlStandardService.EnsureCreditNr(ai, creditClient, applicationRow, context);
        }
    }
}