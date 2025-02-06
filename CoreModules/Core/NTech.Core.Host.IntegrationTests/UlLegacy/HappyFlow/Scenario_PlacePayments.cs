using nCredit.Code.Cm1;
using nCustomer;
using nCustomer.Code.Services.Aml.Cm1;
using NTech.Core.Credit.Database;
using NTech.Core.Host.IntegrationTests.UlLegacy;
using NTech.Core.Host.IntegrationTests.UlLegacy.Utilities;
using NTech.Core.Module.Shared.Clients;
using NTech.Services.Infrastructure;
using System.Xml.Linq;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class UlLegacyScenarioTests
    {
        private void PlacePayments(UlLegacyTestRunner.TestSupport support)
        {
            var creditNr = CreditsUlLegacy.GetCreateCredit(support, 1).CreditNr;

            Credits.CreateAndPlaceUnplacedPayment(support, creditNr, 50m);
            Credits.CreateAndImportPaymentFileComplex(support, new Dictionary<string, decimal>
            {
                [creditNr] = 100m
            }, payerNameByCreditNr: new Dictionary<string, string>
            {
                [creditNr] = "Pay Er"
            });
            int importBusinessEventId;
            using(var context = support.CreateCreditContextFactory().CreateContext())
            {
                importBusinessEventId = context.BusinessEventsQueryable.Where(x => x.EventType == "NewIncomingPaymentFile").OrderByDescending(x => x.Id).Select(x => x.Id).First();
            }
            Credits.PlaceUnplacedPaymentWithCreditNrNoteOrOcrReference(support, importBusinessEventId);
        }
    }
}
