using Microsoft.AspNetCore.Mvc;
using PsdTwoPrototype.Apis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PsdTwoPrototype.Controllers
{
    public class HomeController : Controller
    {
        public static Dictionary<string, object> sessionHandler = new Dictionary<string, object>();

        [Route("/DownloadFile")]
        public async Task<IActionResult> Index()
        {
            //1. Create internalRequestId to keep track of session
            var internalRequestId = Guid.NewGuid().ToString();

            //2. Get returnTokens from /BankAccountsAggregations
            var bankAccountsAggregations = new BankAccountsAggregationsController();
            var returnTokens = await bankAccountsAggregations.GetBankAccountsAggregationsReturnTokens(internalRequestId);

            //3. save sessionToken on internalRequestId. Don't save nonce. 
            HomeController.sessionHandler.Add(internalRequestId, returnTokens.Value.sessionToken);

            //4. Redirect to signing with nonce
            return Redirect("https://test.asiakastieto.fi/psd2-client/session/" + returnTokens.Value.nonce);
           
            //5. Root data POSTed to CalculationResultCallback/{internalRequestId}
            //6. Send Root.ruleResponse.RawData back
            //7. Get and Save PDF posted to https://test.asiakastieto.fi/services/psd2-api/bankAccountsAggregations/createRulePDF/
            //8a. SuccessCallback => SuccessRedirectController downloads file (attached to UX flow somewhere)
            //8b. ErrorCallback => Error handling (to-do)
        }
    }
}
