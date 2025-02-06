using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace nCredit.WebserviceMethods.MortgageLoan
{
    public class CreateMortageLoanPropertyCollateralMethod : TypedWebserviceMethod<CreateMortageLoanPropertyCollateralMethod.Request, CreateMortageLoanPropertyCollateralMethod.Response>
    {
        public override string Path => "MortgageLoans/Create-Collateral";

        public override bool IsEnabled => NEnv.IsStandardMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            using (var context = new CreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                var services = requestContext.Service();
                var collateral = services.MortgageLoanCollateral.CreateCollateral(context, request.Properties);

                context.SaveChanges();

                return new Response
                {
                    CollateralId = collateral.Id
                };
            }
        }

        public class Request
        {
            [Required]
            public Dictionary<string, string> Properties { get; set; }
        }

        public class Response
        {
            public int CollateralId { get; set; }
        }
    }
}