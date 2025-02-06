using System;
using System.Web.Mvc;

namespace nPreCredit.Code.Services
{

    public class HttpContextUrlService : IHttpContextUrlService
    {
        private UrlHelper urlHelper;

        public HttpContextUrlService(UrlHelper urlHelper)
        {
            this.urlHelper = urlHelper;
        }

        public string ActionStrict(string actionName, string controllerName, object routeValues = null)
        {
            string result;

            if (routeValues == null)
                result = urlHelper.Action(actionName, controllerName);
            else
                result = urlHelper.Action(actionName, controllerName, routeValues);

            if (result == null)
                throw new Exception($"Route {controllerName}.{actionName} does not exist");

            return result;
        }

        public string ArchiveDocumentUrl(string archiveKey)
        {
            return ActionStrict("ArchiveDocument", "CreditManagement", new { key = archiveKey });
        }

        public string ApplicationUrl(string applicationNr, bool isMortgageLoanApplication)
        {
            var routeValues = new { applicationNr };
            return isMortgageLoanApplication
                ? urlHelper.ActionStrict("Index", "MortgageLoanApplication", routeValues: routeValues)
                : urlHelper.ActionStrict("CreditApplication", "CreditManagement", routeValues: routeValues);
        }
    }

    public interface IHttpContextUrlService
    {
        string ActionStrict(string actionName, string controllerName, object routeValues = null);
        string ArchiveDocumentUrl(string archiveKey);
        string ApplicationUrl(string applicationNr, bool isMortgageLoanApplication);
    }
}