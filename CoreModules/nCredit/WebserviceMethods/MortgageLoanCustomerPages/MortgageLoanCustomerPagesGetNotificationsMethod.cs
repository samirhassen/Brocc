using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.WebserviceMethods.MortgageLoanCustomerPages
{
    public class MortgageLoanCustomerPagesGetNotificationsMethod : MortgageLoanCustomerPagesMethod<MortgageLoanCustomerPagesGetNotificationsMethod.Request, MortgageLoanCustomerPagesGetNotificationsMethod.Response>
    {
        protected override string MethodName => "notifications";

        protected override Response DoCustomerLockedExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request, int customerPagesUserCustomerId)
        {
            var incomingPaymentAccount = requestContext.Service().PaymentAccount.GetIncomingPaymentBankAccountNr();
            using (var context = new CreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                var notifications = Controllers.ApiCustomerPagesController.GetOpenNotifications(customerPagesUserCustomerId, requestContext.Clock(), context, incomingPaymentAccount,
                    requestContext.Service().PaymentOrder);

                if (!string.IsNullOrWhiteSpace(request.OnlyForLoanNr))
                    notifications = notifications.Where(x => x.CreditNr == request.OnlyForLoanNr).ToList();

                var creditNrs = notifications.Select(x => x.CreditNr).ToHashSet();

                var creditData = Controllers
                    .ApiCustomerPagesController
                    .GetCustomerFacingCreditModels(context, customerPagesUserCustomerId)
                    .Where(x => creditNrs.Contains(x.CreditNr))
                    .ToList()
                    .ToDictionary(x => x.CreditNr, x => x);

                var m = new List<Response.NotificationModel>();
                foreach (var n in notifications)
                {
                    var cd = creditData[n.CreditNr];
                    var isDirectDebitActive = cd.IsDirectDebitActive == "true";
                    m.Add(new Response.NotificationModel
                    {
                        LoanNr = n.CreditNr,
                        DirectDebitBankAccoutNr = isDirectDebitActive ? cd.DirectDebitBankAccountNr : null,
                        WillBePaidByDirectDebit = n.TotalUnpaidNotifiedAmount > 0
                            && isDirectDebitActive
                            && (!n.IsOverdue || (n.IsOverdue && requestContext.Clock().Today <= n.DueDate.AddDays(3))),
                        TotalUnpaidNotifiedAmount = n.TotalUnpaidNotifiedAmount,
                        Documents = n.Documents.Select(x => new Response.DocumentModel
                        {
                            DocumentId = x.DocumentId,
                            DocumentType = x.DocumentType
                        }).ToList(),
                        DueDate = n.DueDate,
                        Id = n.Id,
                        IsOverdue = n.IsOverdue,
                        OcrPaymentReference = n.OcrPaymentReference,
                        PaymentBankGiro = n.PaymentBankGiro
                    });
                }

                return new Response
                {
                    Notifications = m
                };
            }
        }

        public class Request : MortgageLoanCustomerPagesRequestBase
        {
            public string OnlyForLoanNr { get; set; }
        }

        public class Response
        {
            public List<NotificationModel> Notifications { get; set; }
            public class DocumentModel
            {
                public string DocumentType { get; set; }
                public string DocumentId { get; set; }
            }
            public class NotificationModel
            {
                public int Id { get; set; }
                public string LoanNr { get; set; }
                public DateTime DueDate { get; set; }
                public bool IsOverdue { get; set; }
                public bool WillBePaidByDirectDebit { get; set; }
                public string DirectDebitBankAccoutNr { get; set; }
                public decimal TotalUnpaidNotifiedAmount { get; set; }
                public string OcrPaymentReference { get; set; }
                public string PaymentBankGiro { get; set; }
                public List<DocumentModel> Documents { get; set; }
            }
        }
    }
}