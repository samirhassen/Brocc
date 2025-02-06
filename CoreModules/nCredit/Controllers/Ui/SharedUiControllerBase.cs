using nCredit.Code;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    public abstract class SharedUiControllerBase : NController
    {
        protected virtual void ExtendParameters(IDictionary<string, object> p)
        {
        }

        protected ActionResult RenderComponent(
           string componentName,
           string pageTitle,
           Dictionary<string, object> additionalParameters = null)
        {
            if (!IsEnabled)
                return HttpNotFound();

            var initialData = new ExpandoObject();

            initialData.SetValues(x =>
            {
                x["today"] = Clock.Today.ToString("yyyy-MM-dd");
                x["isTest"] = !NEnv.IsProduction;
                x["currentUserId"] = this.CurrentUserId;
                x["backofficeMenuUrl"] = new Uri(NEnv.ServiceRegistry.External["nBackoffice"]).ToString();
                x["customerCardUrlPattern"] = CreditCustomerClient.GetCustomerCardUrl("[[[CUSTOMER_ID]]]", NTechNavigationTarget.CreateFromTargetCode("[[[BACK_TARGET]]]"));
                x["userDisplayNameByUserId"] = this.Service.UserDisplayName.GetUserDisplayNamesByUserId();
                x["providers"] = NEnv.GetAffiliateModels();
                x["crossModuleNavigateUrlPattern"] = NEnv.ServiceRegistry.External.ServiceUrl("nBackoffice", "Ui/CrossModuleNavigate", Tuple.Create("targetCode", "[[[TARGET_CODE]]]"));

                ExtendParameters(x);

                if (additionalParameters != null)
                {
                    foreach (var v in additionalParameters)
                        x[v.Key] = v.Value;
                }
            });

            ViewBag.Title = pageTitle;
            ViewBag.ComponentName = componentName;
            SetInitialData(initialData);

            return View("ComponentHostUi");
        }
    }
}