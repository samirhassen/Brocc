using NTech.Services.Infrastructure;
using System.Collections.Generic;

namespace nCredit.Controllers
{
    [NTechAuthorizeMortgageLoanMiddle]
    public class MortgageLoansMiddleSharedUiController : SharedUiControllerBase
    {
        protected override bool IsEnabled => NEnv.IsMortgageLoansEnabled;

        protected override void ExtendParameters(IDictionary<string, object> p)
        {

        }
    }
}