using nPreCredit.Code;
using nPreCredit.Code.Services;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    [RoutePrefix("api/FraudControl")]
    public class FraudControlController : NController
    {
        private readonly IFraudModelService fraudModelService;

        public FraudControlController(IFraudModelService fraudModelService)
        {
            this.fraudModelService = fraudModelService;
        }

        [HttpPost]
        [Route("FetchModel")]
        public ActionResult FetchModel(string applicationNr)
        {
            return Json2(fraudModelService.GetFraudControlModel(applicationNr));
        }

        [HttpPost]
        [Route("GetFraudControls")]
        public ActionResult GetFraudControls(string applicationNr)
        {
            using (var context = new PreCreditContext())
            {
                var fraudEngine = new StandardFraudControlEngine(null, context);
                var controls = fraudEngine.GetFraudControls(applicationNr);
                return Json2(controls);
            }
        }

        [HttpPost]
        [Route("RunFraudControls")]
        public ActionResult RunFraudControls(string applicationNr)
        {
            if (!NEnv.IsStandardUnsecuredLoansEnabled)
            {
                throw new NotSupportedException("Only usable by unsecured loans standard. ");
            }

            var customerClient = new PreCreditCustomerClient();
            RunFraudControlsResult result;

            using (var context = new PreCreditContext())
            {
                var fraudControls = new List<string> { "SameAddressCheck", "SameEmailCheck", "SameAccountNrCheck" };
                var fraudEngine = new StandardFraudControlEngine(customerClient, context);
                result = fraudEngine.RunFraudChecks(applicationNr, fraudControls);

                fraudEngine.SaveFraudControls(applicationNr, result.FraudControls, InformationMetadata, CurrentUserId, Clock.Now);
                context.SaveChanges();
            }


            return Json2(result);
        }

        [HttpPost]
        [Route("SetFraudControlItemApproved")]
        public ActionResult SetFraudControlApproved(string fraudControlName, string applicationNr)
        {
            using (var context = new PreCreditContext())
            {
                var fraudEngine = new StandardFraudControlEngine(null, context);
                fraudEngine.SetFraudControlItemStatusApproved(fraudControlName, applicationNr);
                fraudEngine.UpdateControlStatusBasedOnChildItems(applicationNr);
                context.SaveChanges();
            }

            return new EmptyResult();
        }

        [HttpPost]
        [Route("SetFraudControlItemInitial")]
        public ActionResult SetFraudControlInitial(string fraudControlName, string applicationNr)
        {
            using (var context = new PreCreditContext())
            {
                var fraudEngine = new StandardFraudControlEngine(null, context);
                fraudEngine.SetFraudControlItemStatusInitial(fraudControlName, applicationNr);
                fraudEngine.UpdateControlStatusBasedOnChildItems(applicationNr);
                context.SaveChanges();
            }

            return new EmptyResult();
        }

    }
}