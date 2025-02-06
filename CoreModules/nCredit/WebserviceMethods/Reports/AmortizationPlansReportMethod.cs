using nCredit.Code;
using nCredit.DomainModel;
using nCredit.Excel;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.WebserviceMethods.Reports
{
    public class AmortizationPlansReportMethod : FileStreamWebserviceMethod<AmortizationPlansReportMethod.Request>
    {
        public override string Path => "Reports/GetAmortizationPlans";

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            string[] activeCreditNrs;
            using (var context = requestContext.Service().ContextFactory.CreateContext())
            {
                activeCreditNrs = context
                    .CreditHeadersQueryable
                    .Where(x => x.Status == CreditStatus.Normal.ToString())
                    .OrderBy(x => x.CreditNr)
                    .Select(x => x.CreditNr)
                    .ToArray();
            }

            List<LoanAmortModel> loans = new List<LoanAmortModel>(activeCreditNrs.Length);
            foreach (var creditNrGroup in activeCreditNrs.SplitIntoGroupsOfN(200))
            {
                using (var context = requestContext.Service().ContextFactory.CreateContext())
                {
                    var histories = AmortizationPlan.GetHistoricalCreditModels(creditNrGroup.ToArray(), context, NEnv.IsMortgageLoansEnabled, context.CoreClock.Today);
                    foreach (var creditNr in creditNrGroup)
                    {
                        var creditHistory = histories[creditNr];

                        if (AmortizationPlan.TryGetAmortizationPlan(
                            creditHistory, NEnv.NotificationProcessSettings.GetByCreditType(creditHistory.GetCreditType()),
                            out var plan, out var failedMessage, CoreClock.SharedInstance, NEnv.ClientCfgCore,
                            CreditDomainModel.GetInterestDividerOverrideByCode(NEnv.ClientInterestModel)))
                        {
                            loans.Add(new LoanAmortModel
                            {
                                CreditNr = creditNr,
                                AnnuityAmount = plan.AmortizationModel.UsesAnnuities ? plan.AmortizationModel.GetActualAnnuityOrException() : new decimal?(),
                                RepaymentMonthCount = plan.Items.Where(x => x.IsFutureItem).Count()
                            });
                        }
                        else
                        {
                            loans.Add(new LoanAmortModel
                            {
                                CreditNr = creditNr,
                                AnnuityAmount = null,
                                RepaymentMonthCount = null,
                                ErrorText = failedMessage
                            });
                        }
                    }
                }
            }

            var excelRequest = new DocumentClientExcelRequest
            {
                Sheets = new DocumentClientExcelRequest.Sheet[]
                {
                    new DocumentClientExcelRequest.Sheet
                    {
                        AutoSizeColumns = true,
                        Title = $"Amortization plans {requestContext.Clock().Today.ToString("yyyy-MM-dd")}"
                    }
                }
            };

            var s = excelRequest.Sheets[0];
            s.SetColumnsAndData(loans,
                loans.Col(x => x.CreditNr, ExcelType.Text, "Credit nr"),
                loans.Col(x => x.AnnuityAmount, ExcelType.Number, "Annuity"),
                loans.Col(x => x.RepaymentMonthCount, ExcelType.Number, "Repayment months", nrOfDecimals: 0),
                loans.Col(x => x.ErrorText, ExcelType.Text, "Error"));

            var client = requestContext.Service().DocumentClientHttpContext;
            var result = client.CreateXlsx(excelRequest);

            return ExcelFile(result, downloadFileName: $"AmortizationPlans-{requestContext.Clock().Today.ToString("yyyy-MM-dd")}.xlsx");
        }

        private class LoanAmortModel
        {
            public string CreditNr { get; set; }
            public decimal? AnnuityAmount { get; set; }
            public int? RepaymentMonthCount { get; set; }
            public string ErrorText { get; set; }
        }


        public class Request
        {

        }
    }
}