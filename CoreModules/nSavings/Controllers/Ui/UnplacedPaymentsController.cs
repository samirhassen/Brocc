using nSavings.DbModel.BusinessEvents;
using NTech.Services.Infrastructure;
using System.Linq;
using System.Web.Mvc;

namespace nSavings.Controllers
{
    [NTechAuthorizeSavingsMiddle]
    public class UnplacedPaymentsController : NController
    {
        [HttpGet]
        [Route("Ui/UnplacedPayments/List")]
        public ActionResult Index()
        {
            using (var context = new SavingsContext())
            {
                var unplacedPayments = PaymentDomainModel.CreateForAllNotFullyPlaced(context, 
                    Service.GetEncryptionService(GetCurrentUserMetadata()),
                    IncomingPaymentHeaderItemCode.NoteText, IncomingPaymentHeaderItemCode.OcrReference);

                ViewBag.JsonInitialData = this.EncodeInitialData(new
                {
                    payments = unplacedPayments
                        .Select(x => new
                        {
                            x.PaymentDate,
                            NoteText = x.GetItem(IncomingPaymentHeaderItemCode.NoteText),
                            OcrReference = x.GetItem(IncomingPaymentHeaderItemCode.OcrReference),
                            UnplacedAmount = x.GetUnplacedAmount(Clock.Today),
                            NavigationLink = Url.Action("Place", "UnplacedPayments", new { paymentId = x.PaymentId })
                        })
                        .OrderBy(x => x.PaymentDate)
                        .ToList()
                });
                return View();
            }
        }

        [HttpGet]
        [Route("Ui/UnplacedPayments/Place")]
        public ActionResult Place(int paymentId)
        {
            using (var context = new SavingsContext())
            {
                var unplacedPayment = PaymentDomainModel.CreateForSinglePayment(paymentId, context, Service.GetEncryptionService(GetCurrentUserMetadata()),
                    IncomingPaymentHeaderItemCode.OcrReference);
                var ocr = unplacedPayment.GetItem(IncomingPaymentHeaderItemCode.OcrReference);
                string matchedSavingsAccountNrs = null;
                if (ocr != null)
                {
                    matchedSavingsAccountNrs = string.Join(", ", context
                        .SavingsAccountHeaders
                        .Where(x => x.DatedStrings.Any(y => y.Name == DatedSavingsAccountStringCode.OcrDepositReference.ToString() && y.Value == ocr))
                        .Select(x => x.SavingsAccountNr).ToList());
                }

                var paymentItems = context
                    .IncomingPaymentHeaderItems
                    .Where(x => x.IncomingPaymentHeaderId == paymentId)
                    .Select(x => new
                    {
                        x.Id,
                        x.Name,
                        x.Value,
                        x.IsEncrypted
                    })
                    .ToList()
                    .Select(x => new
                    {
                        ItemId = x.Id,
                        x.Name,
                        x.IsEncrypted,
                        Value = x.IsEncrypted ? null : x.Value
                    })
                    .ToList();

                ViewBag.JsonInitialData = this.EncodeInitialData(new
                {
                    payment = new
                    {
                        Id = unplacedPayment.PaymentId,
                        Items = paymentItems,
                        MatchedSavingsAccountNrs = matchedSavingsAccountNrs,
                        PaymentDate = unplacedPayment.PaymentDate,
                        UnplacedAmount = unplacedPayment.GetUnplacedAmount(Clock.Today)
                    },
                    findSavingsAccountByReferenceNrOrSavingsAccountNrUrl = Url.Action("FindByReferenceNrOrSavingsAccountNr", "ApiUnplacedPayments"),
                    fetchEncryptedPaymentItemValue = Url.Action("FetchEncryptedPaymentItemValue", "ApiUnplacedPayments"),
                    paymentPlacementSuggestionUrl = Url.Action("PaymentPlacementSuggestion", "ApiUnplacedPayments"),
                    placePaymentUrl = Url.Action("PlacePayment", "ApiUnplacedPayments"),
                    repayPaymentUrl = Url.Action("RepayPayment", "ApiUnplacedPayments"),
                    afterPlacementUrl = Url.Action("Index", "UnplacedPayments"),
                    validateAccountNrUrl = Url.Action("ValidateBankAccount", "ApiUnplacedPayments")
                });
                return View();
            }
        }
    }
}