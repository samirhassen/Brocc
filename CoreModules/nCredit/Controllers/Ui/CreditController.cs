using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using System;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechAuthorizeCreditMiddle]
    public class CreditController : NController
    {
        [Route("Ui/Credit")]
        public ActionResult Index(string creditNr, string initialTab, string backTarget)
        {
            var url = NEnv.ServiceRegistry.Internal.ServiceUrl("nBackoffice", string.IsNullOrWhiteSpace(creditNr)
                ? "s/credit/search"
                : $"s/credit/{initialTab ?? "details"}/{creditNr}",
                Tuple.Create("backTarget", backTarget));
            return Redirect(url.ToString());
        }

        [Route("Api/CreateCustomerPagesLoginLink")]
        [HttpPost]
        public ActionResult CreateCustomerPagesOneTimeLoginLink(int customerId)
        {
            if (!NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.customer.allowcredittokenlogin"))
                return HttpNotFound();

            var c = new Code.CreditCustomerClient();
            var n = c.GetCustomerCardItems(customerId, "firstName");

            using (var context = new CreditContext())
            {
                var now = Clock.Now;
                var t = new OneTimeToken
                {
                    ChangedById = CurrentUserId,
                    ChangedDate = now,
                    CreatedBy = CurrentUserId,
                    CreationDate = now,
                    InformationMetaData = InformationMetadata,
                    Token = OneTimeToken.GenerateUniqueToken(),
                    TokenType = "CustomerPagesLogin",
                    ValidUntilDate = now.AddDays(3),
                    TokenExtraData = JsonConvert.SerializeObject(new
                    {
                        CustomerId = customerId,
                        FirstName = n.ContainsKey("firstName") ? n["firstName"] : ""
                    })
                };
                context.OneTimeTokens.Add(t);

                context.SaveChanges();

                var uri = NEnv.ServiceRegistry.External.ServiceUrl("nCustomerPages", "login/credittoken", Tuple.Create("token", t.Token));

                return CreatePartialViewUserMessage("Customer pages", "Onetime login link", link: uri);
            }
        }
    }
}