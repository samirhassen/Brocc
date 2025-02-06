using nPreCredit.Code.Services;
using nPreCredit.Code.Services.CompanyLoans;
using NTech.Services.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    [RoutePrefix("api/ApplicationInfo")]
    public class ApplicationInfoController : NController
    {
        [HttpPost]
        [Route("Fetch")]
        public ActionResult Fetch(string applicationNr)
        {
            return Json2(Service.Resolve<ApplicationInfoService>().GetApplicationInfo(applicationNr));
        }

        [HttpPost]
        [Route("FetchBulk")]
        public ActionResult FetchBulk(List<string> applicationNrs)
        {
            var applicationInfoByApplicationNr = Service.Resolve<ApplicationInfoService>().GetApplicationInfoBatch((applicationNrs ?? new List<string>()).ToHashSet());
            return Json2(applicationInfoByApplicationNr);
        }

        [HttpPost]
        [Route("FetchWithApplicants")]
        public ActionResult FetchWithApplicants(string applicationNr)
        {
            return FetchWithCustom(applicationNr, true, false);
        }

        [HttpPost]
        [Route("FetchWithCustom")]
        public ActionResult FetchWithCustom(string applicationNr, bool? includeAppliants, bool? includeWorkflowStepOrder)
        {
            var s = Service.Resolve<ApplicationInfoService>();
            var i = s.GetApplicationInfo(applicationNr);
            List<string> workflowStepOrder = null;
            if (includeWorkflowStepOrder.GetValueOrDefault() && i?.ApplicationType == CreditApplicationTypeCode.companyLoan.ToString())
            {
                workflowStepOrder = Service.Resolve<ICompanyLoanWorkflowService>().GetStepOrder();
            }
            return Json2(new
            {
                Info = i,
                CustomerIdByApplicantNr = includeAppliants.GetValueOrDefault() ? s.GetApplicationApplicants(applicationNr)?.CustomerIdByApplicantNr : null,
                WorkflowStepOrder = workflowStepOrder
            });
        }

        [HttpPost]
        [Route("FetchExternalRequestJson")]
        public ActionResult FetchExternalRequestJson(string applicationNr)
        {
            return Json2(new
            {
                ExternalApplicationRequestJson = Service.Resolve<IKeyValueStoreService>().GetValue(applicationNr, KeyValueStoreKeySpaceCode.ExternalApplicationRequestJson.ToString())
            });
        }

        [HttpPost]
        [Route("FetchApplicants")]
        public ActionResult FetchApplicants(string applicationNr)
        {
            return Json2(Service.Resolve<ApplicationInfoService>().GetApplicationApplicants(applicationNr));
        }
    }
}