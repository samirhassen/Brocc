﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using nCustomerPages.Code;
using Newtonsoft.Json;
using NTech.Services.Infrastructure;

namespace nCustomerPages.Controllers;

[RoutePrefix("login")]
public class EidSignatureLoginController : BaseController
{
    private readonly Lazy<CommonElectronicIdLoginProvider> _electronicIdLoginProvider =
        new(() => new CommonElectronicIdLoginProvider());

    protected override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        if (!NEnv.IsDirectEidAuthenticationModeEnabled)
        {
            filterContext.Result = HttpNotFound();
        }

        base.OnActionExecuting(filterContext);
    }

    [AllowAnonymous]
    [Route("eid")]
    [PreventBackButton]
    public ActionResult LoginWithEid(string targetName, string targetCustomData, bool skipPrePopulateInTest = false)
    {
        ViewBag.HideUserHeader = true;
        if (targetName is nameof(CustomerNavigationTargetName.ApplicationsOverview)
            or nameof(CustomerNavigationTargetName.Application))
        {
            ViewBag.HideHeader = true;
        }

        ViewBag.TargetName = targetName;
        ViewBag.TargetCustomData = targetCustomData;

        var externalVariables =
            AffiliateTrackingModel.ExternalApplicationVariablesToString(
                AffiliateTrackingModel.ExtractExternalApplicationVariablesFromRequest(this.Request));

        ViewBag.ExternalApplicationVariables = externalVariables == null
            ? null
            : Convert.ToBase64String(Encoding.UTF8.GetBytes(externalVariables));

        var viewName = "LoginWithEidSignature";
        string prePopulateCivicRegNr = null;
        switch (targetName)
        {
            case nameof(CustomerNavigationTargetName.ApplicationsOverview):
                viewName = "LoginWithEidSignature_ApplicationsOverview";
                break;
            case nameof(CustomerNavigationTargetName.ContinueMortgageLoanApplication):
            {
                viewName = "LoginWithEidSignature_ContinueMortgageLoanApplication";
                if (!NEnv.IsProduction && !skipPrePopulateInTest)
                {
                    //To make tesing easier we prepopulate this
                    var testClient = new SystemUserTestClient();
                    prePopulateCivicRegNr = testClient.GenerateTestPersons(isAccepted: true).Single()["civicRegNr"];
                }

                break;
            }
            case nameof(CustomerNavigationTargetName.StandardOverview):
            case nameof(CustomerNavigationTargetName.SecureMessages) when
                NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.unsecuredloans.standard"):
                viewName = "LoginWithEidSignature_StandardOverview";
                break;
            case nameof(CustomerNavigationTargetName.SavingsStandardApplication):
                viewName = "LoginWithEidSignature_SavingsStandardApplication";
                break;
        }

        ViewBag.JsonInitialData = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
        {
            translation = GetTranslations(),
            targetName = targetName,
            targetCustomData = targetCustomData,
            isSavingsApplicationActive = NEnv.IsSavingsApplicationActive,
            baseCountry = NEnv.ClientCfg.Country.BaseCountry,
            prePopulateCivicRegNr = prePopulateCivicRegNr
        })));

        return View(viewName);
    }

    [AllowAnonymous]
    [Route("eid-signature")]
    public ActionResult LoginWithEidSignature(string targetName, string targetCustomData)
    {
        //NOTE: The signature bit is a legacy name from the original provider that only supported signature based eid logins.
        //      The scrive provider has never used this but signicat has so make sure any clients dont depend on this before removing it.
        return LoginWithEid(targetName, targetCustomData);
    }

    [AllowAnonymous]
    [HttpPost]
    [Route("eid-signature-start")]
    [NTechHardenedMvcModelBinderAllowForms]
    public ActionResult StartLoginOld(string civicRegNr, string targetName, string targetCustomData,
        string externalApplicationVariables)
    {
        //See LoginWithEidSignature for why this exists
        return StartLogin(civicRegNr, targetName, targetCustomData, externalApplicationVariables);
    }

    [AllowAnonymous]
    [HttpPost]
    [Route("eid-start")]
    [NTechHardenedMvcModelBinderAllowForms]
    public ActionResult StartLogin(string civicRegNr, string targetName, string targetCustomData,
        string externalApplicationVariables)
    {
        string additionalData = null;
        if (externalApplicationVariables != null || targetCustomData != null)
        {
            var externalApplicationVariablesDecoded =
                Encoding.UTF8.GetString(Convert.FromBase64String(externalApplicationVariables));
            additionalData = "V2:" + JsonConvert.SerializeObject(new
            {
                externalApplicationVariables = externalApplicationVariablesDecoded,
                targetCustomData
            });
        }

        var url = _electronicIdLoginProvider.Value.StartLoginSessionReturningLoginUrl(civicRegNr, targetName,
            additionalData);

        return Redirect(url);
    }

    public static Dictionary<string, string> QueryParamsAndExtras(HttpRequestBase request,
        params Tuple<string, string>[] extras)
    {
        var d = new Dictionary<string, string>();
        foreach (var key in request.QueryString.AllKeys)
        {
            var value = request.QueryString[key];
            if (!string.IsNullOrWhiteSpace(value))
                d[key] = value;
        }

        foreach (var item in extras)
            d[item.Item1] = item.Item2;
        return d;
    }

    private ActionResult HandleAuthenticationReturn(Dictionary<string, string> providerParameters)
    {
        var result = _electronicIdLoginProvider.Value.GetLoginSessionResult(providerParameters);

        if (!result.IsSuccess) return RedirectToAction("AccessDenied", "Common");

        //Login
        var cc = new SystemUserCustomerClient();
        var customerId = cc.GetCustomerId(NEnv.BaseCivicRegNumberParser.Parse(result.CivicNr));

        //Get customer if exists
        var firstNameItems = cc.GetCustomerCardItems(customerId, "firstName");
        string firstName = null;
        if (firstNameItems.TryGetValue("firstName", out var item))
            firstName = item;

        string extVarKey = null;
        string targetCustomData = null;
        if (result.AdditionalData != null)
        {
            string externalApplicationVariablesFromAdditionalData;
            if (result.AdditionalData.StartsWith("V2:"))
            {
                var additionalData = JsonConvert.DeserializeAnonymousType(
                    result.AdditionalData.Substring("V2:".Length),
                    new { externalApplicationVariables = "", targetCustomData = "" });
                externalApplicationVariablesFromAdditionalData = additionalData?.externalApplicationVariables;
                targetCustomData = additionalData?.targetCustomData;
            }
            else
            {
                externalApplicationVariablesFromAdditionalData = result.AdditionalData;
            }

            var externalApplicationVariables =
                AffiliateTrackingModel.GetExternalApplicationVariablesFromString(
                    externalApplicationVariablesFromAdditionalData);
            if (externalApplicationVariables is { Count: > 0 })
            {
                var c = new SystemUserSavingsClient();
                extVarKey = c.StoreTemporarilyEncryptedData(externalApplicationVariablesFromAdditionalData,
                    expireAfterHours: 48);
            }
        }

        var p = new LoginProvider();
        (string Name, string CustomData)? reloginTarget = null;
        if (result.TargetName != null)
        {
            reloginTarget = (result.TargetName, targetCustomData);
        }

        p.SignIn(HttpContext.GetOwinContext(), customerId, firstName, true,
            _electronicIdLoginProvider.Value.AuthTypeName,
            NEnv.BaseCivicRegNumberParser.Parse(result.CivicNr), reloginTarget);

        Session["EidSignatureCustomerTarget"] = reloginTarget?.Name;

        return RedirectToAction("Navigate", "CustomerPortal",
            new
            {
                targetName = reloginTarget?.Name, targetCustomData = reloginTarget?.CustomData,
                extVarKey = extVarKey
            });
    }

    [AllowAnonymous]
    [Route("eid-signature-return")]
    [HttpGet]
    public ActionResult ReturnFromSignature()
    {
        return HandleAuthenticationReturn(QueryParamsAndExtras(Request));
    }

    /// <summary>
    /// Basically the same as ReturnFromSignature but allow us to include a local session id in the url rather than the query string
    /// which should allow it to be used for all providers that dont have explicit whitelists for exact urls.
    /// It's at least used for mock and scrive as of this writing.
    /// </summary>
    [AllowAnonymous]
    [Route("eid/{localSessionId}/return")]
    [HttpGet]
    public ActionResult ReturnAfterAuthentication(string localSessionId)
    {
        return HandleAuthenticationReturn(QueryParamsAndExtras(Request,
            Tuple.Create("localSessionId", localSessionId)));
    }
}