using nCredit.DomainModel;
using NTech.Services.Infrastructure;
using System.Linq;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechAuthorizeCreditMiddle]
    public class UnplacedPaymentsController : NController
    {
        [HttpGet]
        [Route("Ui/UnplacedPayments/List")]
        public ActionResult Index()
        {
            using (var context = new CreditContextExtended(this.GetCurrentUserMetadata(), this.Clock))
            {
                var unplacedPayments = PaymentDomainModel.CreateForAllNotFullyPlaced(context, CreateEncryptionService(), IncomingPaymentHeaderItemCode.NoteText, IncomingPaymentHeaderItemCode.OcrReference);

                SetInitialData(new
                {
                    payments = unplacedPayments
                        .Select(x => new
                        {
                            x.PaymentDate,
                            NoteText = x.GetItem(IncomingPaymentHeaderItemCode.NoteText),
                            OcrReference = x.GetItem(IncomingPaymentHeaderItemCode.OcrReference),
                            UnplacedAmount = x.GetUnplacedAmount(Clock.Today),
                            NavigationLink = NEnv.ServiceRegistry.Internal.ServiceUrl("nBackOffice", $"s/credit-payments/handle-unplaced/{x.PaymentId}")
                        })
                        .OrderBy(x => x.PaymentDate)
                        .ToList()
                });

                return View();
            }
        }
    }
}