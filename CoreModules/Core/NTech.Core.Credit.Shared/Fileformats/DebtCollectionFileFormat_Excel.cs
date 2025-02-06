using nCredit.DbModel.BusinessEvents;
using nCredit.DomainModel;
using nCredit.Excel;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.Clients;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace nCredit.Code.Fileformats
{
    public class DebtCollectionFileFormat_Excel
    {
        private T[] SkipNulls<T>(params T[] args) where T : class
        {
            if (args == null) return null;
            return args.Where(x => x != null).ToArray();
        }

        private class DebtColCustomer
        {
            public DebtCollectionFileModel.Customer Customer { get; set; }
            public List<string> Roles { get; set; }
        }

        public string CreateExcelFileInArchive(DebtCollectionFileModel f, DateTimeOffset now, string filename, IDocumentClient documentClient,
            Dictionary<string, DebtCollectionNotNotifiedInterest> notNotifiedInterestPerCreditNr, PaymentOrderService paymentOrderService)
        {
            var request = new DocumentClientExcelRequest
            {
                Sheets = new DocumentClientExcelRequest.Sheet[]
                {
                    new DocumentClientExcelRequest.Sheet
                    {
                        AutoSizeColumns = true,
                        Title = $"Debt col. ({now.ToString("yyyy-MM-dd")})"
                    },
                    new DocumentClientExcelRequest.Sheet
                    {
                        AutoSizeColumns = true,
                        Title = "Not notified interest"
                    }
                }
            };

            //Flatten
            var rows = new List<Tuple<DebtCollectionFileModel.Credit, DebtColCustomer, DebtCollectionFileModel.Notification>>();
            foreach (var credit in f.Credits)
            {
                var creditRow = new ExpandoObject();
                rows.Add(Tuple.Create(credit, (DebtColCustomer)null, (DebtCollectionFileModel.Notification)null));
                foreach (var c in credit.OrderedCustomersWithRoles.Select(x => new DebtColCustomer { Customer = x.Item1, Roles = x.Item2.ToList() }))
                {
                    rows.Add(Tuple.Create((DebtCollectionFileModel.Credit)null, c, (DebtCollectionFileModel.Notification)null));
                }
                foreach (var n in credit.Notifications)
                {
                    rows.Add(Tuple.Create((DebtCollectionFileModel.Credit)null, (DebtColCustomer)null, n));

                }
            }
            var s = request.Sheets[0];
            var columns = DocumentClientExcelRequest.CreateDynamicColumnList(rows);
            //Shared type column
            columns.Add(rows.Col(x => x.Item1 != null ? "Credit" : (x.Item2 != null ? "Customer" : "Notification"), ExcelType.Text, "RowType"));

            //Credit columns          
            columns.Add(rows.Col(x => x.Item1 != null ? x.Item1.CreditNr : null, ExcelType.Text, "CreditNr"));
            columns.Add(rows.Col(x => x.Item1 != null ? x.Item1.InitialLoanCampaignCode : null, ExcelType.Text, "CampaignCode"));
            columns.Add(rows.Col(x => x.Item1 != null ? x.Item1.Ocr : null, ExcelType.Text, "Ocr"));
            columns.Add(rows.Col(x => x.Item1 != null ? new DateTime?(x.Item1.StartDate) : null, ExcelType.Date, "StartDate"));
            columns.Add(rows.Col(x => x.Item1 != null ? new DateTime?(x.Item1.NextInterestDate) : null, ExcelType.Date, "NextInterestDate"));
            columns.Add(rows.Col(x => x.Item1 != null ? new DateTime?(x.Item1.TerminationLetterDueDate) : null, ExcelType.Date, "TerminationLetterDueDate"));
            columns.Add(rows.Col(x => x.Item1 != null ? new decimal?(x.Item1.InterestRatePercent / 100m) : null, ExcelType.Percent, "InterestRatePercent"));
            columns.Add(rows.Col(x => x.Item1 != null ? new decimal?(x.Item1.NotNotifiedCapitalAmount) : null, ExcelType.Number, "NotNotifiedCapitalAmount", nrOfDecimals: 2));
            columns.Add(rows.Col(x => x.Item1 != null ? new decimal?(x.Item1.CapitalizedInitialFeeAmount) : null, ExcelType.Number, "CapitalizedInitialFeeAmount", nrOfDecimals: 2));
            columns.Add(rows.Col(x => x.Item1 != null ? new decimal?(x.Item1.NewCreditCapitalAmount) : null, ExcelType.Number, "NewCreditCapitalAmount", nrOfDecimals: 2));
            columns.Add(rows.Col(x => x.Item1 != null ? new decimal?(x.Item1.AdditionalLoanCapitalAmount) : null, ExcelType.Number, "AdditionalLoanCapitalAmount", nrOfDecimals: 2));

            //Customer columns
            columns.Add(rows.Col(x => x.Item2 != null ? x.Item2.Customer.CivicRegNrOrOrgnr : null, ExcelType.Text, "Cust - CivicRegNr"));
            columns.Add(rows.Col(x => x.Item2 != null ? (x.Item2.Customer.IsCompany ? x.Item2.Customer.CompanyName : x.Item2.Customer.FirstName) : null, ExcelType.Text, "Cust - FirstName"));
            columns.Add(rows.Col(x => x.Item2 != null ? x.Item2.Customer.LastName : null, ExcelType.Text, "Cust - LastName"));
            columns.Add(rows.Col(x => x.Item2 != null ? x.Item2.Customer.Email : null, ExcelType.Text, "Cust - Email"));
            columns.Add(rows.Col(x => x.Item2 != null ? x.Item2.Customer.Phone : null, ExcelType.Text, "Cust - Phone"));
            columns.Add(rows.Col(x => x.Item2 != null ? x.Item2.Customer.Adr.Street : null, ExcelType.Text, "Cust - Adr street"));
            columns.Add(rows.Col(x => x.Item2 != null ? x.Item2.Customer.Adr.Zipcode : null, ExcelType.Text, "Cust - Adr zipcode"));
            columns.Add(rows.Col(x => x.Item2 != null ? x.Item2.Customer.Adr.City : null, ExcelType.Text, "Cust - Adr city"));
            columns.Add(rows.Col(x => x.Item2 != null ? x.Item2.Customer.Adr.Country : null, ExcelType.Text, "Cust - Adr country"));
            columns.Add(rows.Col(x => x.Item2 != null ? string.Join(",", x.Item2.Roles) : null, ExcelType.Text, "Cust - roles"));

            //Notification columns
            columns.Add(rows.Col(x => x.Item3 != null ? new DateTime?(x.Item3.NotificationDate) : null, ExcelType.Date, "Not. - NotificationDate"));
            columns.Add(rows.Col(x => x.Item3 != null ? new DateTime?(x.Item3.DueDate) : null, ExcelType.Date, "Not. - DueDate"));

            var paymentOrder = paymentOrderService.GetPaymentOrderUiItems();
            foreach (var amt in paymentOrder)
            {
                columns.Add(rows.Col(x => x.Item3 != null ? (x.Item3.Amounts.ContainsKey(amt.UniqueId) ? new decimal?(x.Item3.Amounts[amt.UniqueId]) : new decimal?(0m)) : null, ExcelType.Number, $"Not. - {amt.Text}", nrOfDecimals: 2));
            }

            s.SetColumnsAndData(rows, columns.ToArray());

            PopulateNotNotifiedInterestTab(request.Sheets[1], f, notNotifiedInterestPerCreditNr);

            return documentClient.CreateXlsxToArchive(request, filename);
        }

        private void PopulateNotNotifiedInterestTab(DocumentClientExcelRequest.Sheet sheet,
            DebtCollectionFileModel fileModel,
            Dictionary<string, DebtCollectionNotNotifiedInterest> notNotifiedInterestPerCreditNr)
        {

            //Not notified interest sheet
            var notNotifiedInterestRows = new List<(bool IsText, string Text, string CreditNr, DateTime? FromDate, decimal? Amount, int? NrOfInterestDays)>();
            void AddTextRow(string text) => notNotifiedInterestRows.Add((IsText: true, Text: text, CreditNr: null, FromDate: null, Amount: null, NrOfInterestDays: null));
            void AddInterestRow(string creditNr, DateTime? fromDate, decimal? amount, int? nrOfInterestDays) =>
                notNotifiedInterestRows.Add((IsText: false, Text: null, CreditNr: creditNr, FromDate: fromDate, Amount: amount, NrOfInterestDays: nrOfInterestDays));

            AddTextRow("The standard way to handle this is to send 'Next Interest Date' to the debt collection company and let them compute the not notified interest.");
            AddTextRow("This tab is included if you want to do this yourself instead. Just make sure you don't do both since the interest will then be charged twice.");

            foreach (var credit in fileModel.Credits)
            {
                DateTime? fromDate = null;
                decimal? amount = null;
                int? nrOfInterestDays = null;

                if (notNotifiedInterestPerCreditNr.ContainsKey(credit.CreditNr) && notNotifiedInterestPerCreditNr[credit.CreditNr] != null)
                {
                    fromDate = notNotifiedInterestPerCreditNr[credit.CreditNr].FromDate;
                    amount = notNotifiedInterestPerCreditNr[credit.CreditNr].Amount;
                    nrOfInterestDays = notNotifiedInterestPerCreditNr[credit.CreditNr].NrOfInterestDays;
                }
                AddInterestRow(credit.CreditNr, fromDate, amount, nrOfInterestDays);
            }

            sheet.SetColumnsAndData(notNotifiedInterestRows,
                notNotifiedInterestRows.Col(x => x.IsText ? x.Text : null, ExcelType.Text, ""),
                notNotifiedInterestRows.Col(x => !x.IsText ? x.CreditNr : null, ExcelType.Text, "Credit nr"),
                notNotifiedInterestRows.Col(x => !x.IsText ? x.FromDate : null, ExcelType.Date, "From date"),
                notNotifiedInterestRows.Col(x => !x.IsText ? x.NrOfInterestDays : null, ExcelType.Number, "Interest days", nrOfDecimals: 0),
                notNotifiedInterestRows.Col(x => !x.IsText ? x.Amount : null, ExcelType.Number, "Not notified interest amount", includeSum: true));
        }
    }
}
