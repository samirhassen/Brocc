using nCredit.DbModel.BusinessEvents;
using NTech.Services.Infrastructure;
using System;
using System.Net;
using System.Web.Mvc;


namespace nCredit.Controllers
{
    [NTechApi]
    [NTechAuthorizeCreditMiddle(ValidateAccessToken = true)]
    public class ApiDebtCollectionCandidatesController : NController
    {
        private CreditDebtCollectionBusinessEventManager CreateCreditDebtCollectionBusinessEventManager() => Service.CreditDebtCollectionBusinessEventManager;

        /// <param name="creditNr"></param>
        /// <param name="postponeUntilDate">If this is included postpone, otherwise resume</param>
        [HttpPost]
        [Route("Api/Credit/DebtCollectionCandidates/PostponeOrResume")]
        public ActionResult PostponeOrResume(string creditNr, DateTime? postponeUntilDate)
        {
            using (var context = CreateCreditContext())
            {
                var e = CreateCreditDebtCollectionBusinessEventManager();
                string msg;
                var isOk = e.TryPostponeOrResumeDebtCollection(creditNr, context, postponeUntilDate, out msg);
                context.SaveChanges();
                if (!isOk)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, msg);
                else
                    return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
        }

        [HttpPost]
        [Route("Api/Credit/DebtCollectionCandidates/GetPage")]
        public ActionResult GetPage(int pageSize, int pageNr = 0, string omniSearch = null)
        {
            var result = Service.DebtCollectionCandidate.GetDebtCollectionCandidatesPage(omniSearch, pageSize, pageNr, x => Url.Action("Index", "Credit", new { creditNr = x }, Request.Url.Scheme));
            return Json2(result);
        }
    }
}