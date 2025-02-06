using nCredit.Excel;
using NTech.Banking.BankAccounts;
using NTech.Core.Module.Shared.Clients;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.Code
{
    public class OutgoingPaymentFileFormat_ExcelSe
    {
        public class PaymentFile
        {
            public DateTime ExecutionDate { get; set; }
            public List<PaymentGroup> Groups { get; set; }
            public class PaymentGroup
            {
                public List<Payment> Payments { get; set; }
            }
        }

        public class Payment
        {
            public IBankAccountNumber ToBankAccount { get; set; }
            public string Message { get; set; }
            public string Reference { get; set; }
            public decimal Amount { get; set; }
            public string CustomerName { get; set; }
        }

        public string CreateExcelFileInArchive(PaymentFile f, DateTimeOffset now, string filename, IDocumentClient documentClient)
        {
            var request = new DocumentClientExcelRequest
            {
                Sheets = new DocumentClientExcelRequest.Sheet[]
                {
                    new DocumentClientExcelRequest.Sheet
                    {
                        AutoSizeColumns = true,
                        Title = $"Payments ({now.ToString("yyyy-MM-dd")})"
                    }
                }
            };
            var s = request.Sheets[0];
            var payments = f.Groups.SelectMany(x => x.Payments.Select(y => new
            {
                f.ExecutionDate,
                y.Amount,
                y.ToBankAccount,
                y.CustomerName,
                y.Message,
                y.Reference
            })).ToList();

            s.SetColumnsAndData(payments,
                payments.Col(x => x.ExecutionDate, ExcelType.Date, "Date"),
                payments.Col(x => x.Amount, ExcelType.Number, "Amount", nrOfDecimals: 2, includeSum: true),
                payments.Col(x => AccountTypeName(x.ToBankAccount?.AccountType), ExcelType.Text, "To account type"),
                payments.Col(x => x.ToBankAccount?.FormatFor("display"), ExcelType.Text, "To account"),
                payments.Col(x => x.CustomerName, ExcelType.Text, "Customer name"),
                payments.Col(x => x.Message, ExcelType.Text, "Message"),
                payments.Col(x => x.Reference, ExcelType.Text, "Reference"));

            return documentClient.CreateXlsxToArchive(request, filename);
        }

        private string AccountTypeName(BankAccountNumberTypeCode? t)
        {
            if (!t.HasValue)
                return null;
            switch (t.Value)
            {
                case BankAccountNumberTypeCode.BankAccountSe:
                    return "regular account";
                case BankAccountNumberTypeCode.BankGiroSe:
                    return "bankgiro";
                case BankAccountNumberTypeCode.PlusGiroSe:
                    return "plusgiro";
                default:
                    return t.ToString();
            }
        }
    }
}
