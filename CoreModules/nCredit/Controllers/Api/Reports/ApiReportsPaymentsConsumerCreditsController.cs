﻿using nCredit.Code;
using nCredit.Excel;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    public class ApiReportsPaymentsConsumerCreditsController : NController
    {

        [Route("Api/Reports/GetPaymentsConsumerCredits")]
        [HttpGet()]
        public ActionResult Get(DateTime date)
        {
            if (!NEnv.IsUnsecuredLoansEnabled)
                return HttpNotFound();

            try
            {
                var dc = Service.DocumentClientHttpContext;
                using (var context = new CreditContext())
                {
                    var d = date.Date;
                    var shouldBePaidToCustomerCode = TransactionAccountType.ShouldBePaidToCustomer.ToString();
                    var newOutgoingPaymentFileCode = BusinessEventType.NewOutgoingPaymentFile.ToString();
                    var toAccountCode = NEnv.ClientCfg.Country.BaseCountry == "FI" ? OutgoingPaymentHeaderItemCode.ToIban.ToString() : OutgoingPaymentHeaderItemCode.ToBankAccountNr.ToString();
                    var toCustomerNameCode = OutgoingPaymentHeaderItemCode.CustomerName.ToString();

                    var payments = context
                        .OutgoingPaymentFileHeaders
                        .Where(x => x.TransactionDate == d)
                        .SelectMany(x => x.Payments.Select(y => new
                        {
                            x.TransactionDate,
                            x.FileArchiveKey,
                            y.CreatedByEvent,
                            ShouldBePaidToCustomerAmount = -y.Transactions.Where(z => z.AccountCode == shouldBePaidToCustomerCode && z.BusinessEvent.EventType == newOutgoingPaymentFileCode).Sum(z => (decimal?)z.Amount) ?? 0m,
                            ToAccountItem = y.Items.Where(z => z.Name == toAccountCode).OrderByDescending(z => z.Timestamp).FirstOrDefault(),
                            ToCustomerNameItem = y.Items.Where(z => z.Name == toCustomerNameCode).OrderByDescending(z => z.Timestamp).FirstOrDefault()
                        }))
                        .ToList();

                    var encryptedItems = payments.Select(x => x.ToAccountItem).Concat(payments.Select(x => x.ToCustomerNameItem)).Where(x => x != null && x.IsEncrypted).ToList();

                    IDictionary<long, string> decryptedValues = null;
                    if (encryptedItems.Any())
                    {
                        decryptedValues = EncryptionContext.Load(context, encryptedItems.Select(x => long.Parse(x.Value)).ToArray(), NEnv.EncryptionKeys.AsDictionary());
                    }

                    var fileArchiveKeys = payments.Select(x => x.FileArchiveKey).Where(x => x != null).Distinct().ToList();

                    IDictionary<string, string> fileNameByArchiveKey = new Dictionary<string, string>();
                    foreach (var key in fileArchiveKeys)
                    {
                        var metadata = dc.FetchMetadata(key, true);
                        fileNameByArchiveKey[key] = metadata?.FileName;
                    }

                    var reportPayments = payments.Select(x => new
                    {
                        ToCustomerName = x.ToCustomerNameItem != null ? (x.ToCustomerNameItem.IsEncrypted ? decryptedValues[long.Parse(x.ToCustomerNameItem.Value)] : x.ToCustomerNameItem.Value) : null,
                        ToIban = x.ToAccountItem != null ? (x.ToAccountItem.IsEncrypted ? decryptedValues[long.Parse(x.ToAccountItem.Value)] : x.ToAccountItem.Value) : null,
                        x.TransactionDate,
                        x.ShouldBePaidToCustomerAmount,
                        PaymentFileName = fileNameByArchiveKey.ContainsKey(x.FileArchiveKey) ? fileNameByArchiveKey[x.FileArchiveKey] : "-"
                    }).ToList();


                    var templateFile = NEnv.GetOptionalExcelTemplateFilePath("InitialPaymentReport.xlsx");
                    var templateExists = templateFile != null && templateFile.Exists;

                    var request = new DocumentClientExcelRequest
                    {
                        TemplateXlsxDocumentBytesAsBase64 = templateExists ? Convert.ToBase64String(System.IO.File.ReadAllBytes(templateFile.FullName)) : null,
                        Sheets = new DocumentClientExcelRequest.Sheet[]
                        {
                            new DocumentClientExcelRequest.Sheet
                            {
                                AutoSizeColumns = true,
                                Title = $"Payments consumer credits ({date.ToString("yyyy-MM-dd")})"
                            }
                        }
                    };

                    var s = request.Sheets[0];
                    s.SetColumnsAndData(reportPayments,
                        reportPayments.Col(x => x.ToCustomerName, ExcelType.Text, templateExists ? null : "To customer name"),
                        reportPayments.Col(x => x.ToIban, ExcelType.Text, templateExists ? null : "To account"),
                        reportPayments.Col(x => x.TransactionDate, ExcelType.Date, templateExists ? null : "Transaction date"),
                        reportPayments.Col(x => x.ShouldBePaidToCustomerAmount, ExcelType.Number, templateExists ? null : "Amount", includeSum: true),
                        reportPayments.Col(x => x.PaymentFileName, ExcelType.Text, templateExists ? null : "Payment file"));

                    var result = dc.CreateXlsx(request);

                    return new FileStreamResult(result, XlsxContentType) { FileDownloadName = $"PaymentsConsumerCredits-{date.ToString("yyyy-MM-dd")}.xlsx" };
                }
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Failed to create paymentsConsumerCredits report");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
        }
    }
}