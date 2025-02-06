using nPreCredit.Code;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Web.Mvc;

namespace nPreCredit.Controllers
{
    public abstract class SharedUiControllerBase : NController
    {
        protected virtual void ExtendParameters(IDictionary<string, object> p)
        {
        }

        protected ActionResult RenderComponent(
           string urlToHere,
           string urlToHereFromOtherModule,
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
                x["translation"] = GetTranslations();
                x["isTest"] = !NEnv.IsProduction;

                x["disableBackUrlSupport"] = true;
                x["urlToHere"] = urlToHere;
                x["urlToHereFromOtherModule"] = urlToHereFromOtherModule;
                x["currentUserId"] = this.CurrentUserId;
                x["backofficeMenuUrl"] = new Uri(NEnv.ServiceRegistry.External["nBackoffice"]).ToString();
                x["customerCardUrlPattern"] = PreCreditCustomerClient.GetCustomerCardUrl("[[[CUSTOMER_ID]]]", NTechNavigationTarget.CreateFromTargetCode("[[[BACK_TARGET]]]"));
                x["userDisplayNameByUserId"] = Service.GetService<Code.Services.IUserDisplayNameService>().GetUserDisplayNamesByUserId();

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