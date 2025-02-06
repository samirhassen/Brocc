using nSavings.DbModel.BusinessEvents;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using System;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nSavings.Controllers
{
    [NTechApi]
    public class ApiCreateManualPaymentController : NController
    {
        public class CreateManualPaymentRequest
        {
            public DateTime? BookKeepingDate { get; set; }
            public decimal Amount { get; set; }
            public string NoteText { get; set; }
        }

        [Route("Api/Payments/CreateManual")]
        [HttpPost]
        public ActionResult Create(int? initiatedByUserId, CreateManualPaymentRequest[] requests)
        {
            if (requests == null || requests.Length == 0 || requests.Any(x => x.Amount <= 0m) || requests.Any(x => string.IsNullOrWhiteSpace(x.NoteText)) || requests.Any(x => !x.BookKeepingDate.HasValue))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            if (NEnv.IsProduction)
            {
                if (!initiatedByUserId.HasValue)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing initiatedByUserId");

                if (initiatedByUserId.Value == CurrentUserId)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Duality required");
            }

            using (var context = new SavingsContext())
            {
                var m = new NewManualIncomingPaymentBatchBusinessEventManager(GetCurrentUserMetadata(), CoreClock.SharedInstance, NEnv.ClientCfgCore);
                var payments = requests.Select(x => new NewManualIncomingPaymentBatchBusinessEventManager.ManualPayment
                {
                    Amount = x.Amount,
                    BookkeepingDate = x.BookKeepingDate.Value,
                    NoteText = x.NoteText?.Trim(),
                    InitiatedByUserId = initiatedByUserId
                }).ToArray();
                var evt = m.CreateBatch(context, payments);

                context.SaveChanges();

                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
        }
    }
}