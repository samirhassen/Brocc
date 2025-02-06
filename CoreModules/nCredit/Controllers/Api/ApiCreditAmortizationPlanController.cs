using NTech.Core.Credit.Shared.Services;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    public class ApiCreditAmortizationPlanController : NController
    {
        [HttpGet]
        [Route("Api/Credit/AmortizationPlanPdf")]
        public ActionResult GetAmortizationPlanPdf(string creditNr)
        {
            return GetAmortizationPlanActionResultShared(creditNr, null);
        }

        [HttpGet]
        [Route("Api/Credit/AmortizationPlanPdfWithCustomerCheck")]
        public ActionResult GetAmortizationPlanPdfWithCustomerCheck(string creditNr, int customerId)
        {
            return GetAmortizationPlanActionResultShared(creditNr, customerId);
        }

        private ActionResult GetAmortizationPlanActionResultShared(string creditNr, int? customerId)
        {
            try
            {
                var (p, model) = AmortizationPlanService.GetAmortizationPlanAndModelShared(creditNr, customerId, Service.ContextFactory, NEnv.EnvSettings, 
                    NEnv.NotificationProcessSettings, NEnv.ClientCfgCore);

                var context = new Dictionary<string, object>();
                var culture = CultureInfo.GetCultureInfo(NEnv.ClientCfg.Country.BaseFormattingCulture);

                var annuityAmount = model.AmortizationModel.UsingActualAnnuityOrFixedMonthlyCapital(a => new decimal?(a), _ => new decimal?());
                var fixedAmortizationAmount = model.AmortizationModel.UsingActualAnnuityOrFixedMonthlyCapital(_ => new decimal?(), a => new decimal?(a));

                context["printDate"] = Clock.Today.ToString("d", culture);
                context["creditNr"] = creditNr;
                context["nrOfRemainingPayments"] = p.NrOfRemainingPayments.ToString(culture);
                context["annuityAmount"] = annuityAmount?.ToString("N2", culture);
                context["fixedAmortizationAmount"] = fixedAmortizationAmount?.ToString("N2", culture);

                var trs = p.Items.Select(x =>
                {
                    dynamic e = new ExpandoObject();
                    var ee = e as IDictionary<string, object>;

                    e.date = x.EventTransactionDate.ToString("d", culture);
                    e.capitalBeforeAmount = x.CapitalBefore.ToString("N2", culture);
                    e.isFutureItem = x.IsFutureItem;
                    e.futureItemDueDate = x.FutureItemDueDate;
                    e.isWriteOff = x.IsWriteOff;
                    ee[$"isEvent{x.EventTypeCode}" + (string.IsNullOrWhiteSpace(x.BusinessEventRoleCode) ? "" : $"_{x.BusinessEventRoleCode}")] = true;
                    e.capitalTransactionAmount = x.CapitalTransaction.ToString("N2", culture);
                    e.interestTransactionAmount = x.InterestTransaction.HasValue ? x.InterestTransaction.Value.ToString("N2", culture) : null;
                    e.totalTransactionAmount = x.TotalTransaction.ToString("N2", culture);
                    e.businessEventRoleCode = x.BusinessEventRoleCode;
                    e.isTerminationLetterProcessSuspension = x.IsTerminationLetterProcessSuspension;
                    e.isTerminationLetterProcessReActivation = x.IsTerminationLetterProcessReActivation;

                    return e;
                });

                context["historicTransactions"] = trs.Where(x => !x.isFutureItem).ToList();
                context["futureTransactions"] = trs.Where(x => x.isFutureItem).ToList();

                var service = Service;
                var client = service.DocumentClientHttpContext;                
                
                var result = client.PdfRenderDirect(service.GetPdfTemplate("credit-amortizationplan"), context);
                
                return new FileStreamResult(new MemoryStream(result), "application/pdf") { FileDownloadName = $"AmortizationPlan_{creditNr}_{Clock.Today.ToString("yyyy-MM-dd")}.pdf" };
            }
            catch (NTechWebserviceMethodException ex)
            {
                if (ex.IsUserFacing)
                    return new HttpStatusCodeResult(ex.ErrorHttpStatusCode ?? 400, ex.Message);
                else
                    throw;
            }
        }

        [HttpGet]
        [Route("Api/Credit/MortageLoanAmortizationBasisPdf")]
        public ActionResult GetMortageLoanAmortizationBasisPdf(string creditNr)
        {
            return GetMortgageLoanAmortizationBasisPdfShared(creditNr, null);
        }

        [HttpGet]
        [Route("Api/Credit/MortageLoanAmortizationBasisPdfWithCustomerCheck")]
        public ActionResult GetMortageLoanAmortizationBasisPdfWithCustomerCheck(string creditNr, int customerId)
        {
            return GetMortgageLoanAmortizationBasisPdfShared(creditNr, customerId);
        }

        public ActionResult GetMortgageLoanAmortizationBasisPdfShared(string creditNr, int? customerId)
        {
            if (!NEnv.IsMortgageLoansEnabled)
                return HttpNotFound();
            using (var context = Service.ContextFactory.CreateContext())
            {
                //Check Customer authorized
                if (customerId.HasValue)
                {
                    var creditCustomers = context
                        .CreditCustomersQueryable.Where(x => x.CustomerId == customerId && x.CreditNr == creditNr)
                        .ToList();
                    if (creditCustomers.Count == 0)
                        throw new NTechWebserviceMethodException("Customer not authorized")
                        {
                            ErrorHttpStatusCode = 401,
                            IsUserFacing = true
                        };
                }
            }

            var service = Service;

            var pdfData = SwedishMortgageLoanAmortizationBasisService.GetSwedishAmorteringsunderlagPdfData(
                creditNr,
                CoreClock.SharedInstance,
                service.ContextFactory,
                service.CustomerClientHttpContext,
                service.MortgageLoanCollateral);

            var client = service.DocumentClientHttpContext;
            var result = client.PdfRenderDirect(service.GetPdfTemplate("mortgageloan-amortizationbasis"), pdfData);

            return new FileStreamResult(new MemoryStream(result), "application/pdf") { FileDownloadName = $"MortageLoanAmortizationBasis_{creditNr}_{Clock.Today.ToString("yyyy-MM-dd")}.pdf" };
        }

        private AmortizationPlanService CreateAmortizationPlanService() => new AmortizationPlanService(Service.ContextFactory, NEnv.EnvSettings, CoreClock.SharedInstance,
            NEnv.NotificationProcessSettings, NEnv.ClientCfgCore, GetCurrentUserMetadata());
    }
}