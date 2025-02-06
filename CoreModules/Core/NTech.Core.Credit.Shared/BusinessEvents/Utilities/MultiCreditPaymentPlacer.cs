using nCredit.DomainModel;
using NTech.Banking.IncomingPaymentFiles;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.DbModel.BusinessEvents
{
    public class MultiCreditPaymentPlacer : BusinessEventManagerOrServiceBase
    {
        private readonly ICreditEnvSettings envSettings;
        private readonly PaymentOrderService paymentOrderService;

        public MultiCreditPaymentPlacer(INTechCurrentUserMetadata currentUser, ICoreClock clock, IClientConfigurationCore clientConfiguration,
            ICreditEnvSettings envSettings, PaymentOrderService paymentOrderService) : base(currentUser, clock, clientConfiguration)
        {
            this.envSettings = envSettings;
            this.paymentOrderService = paymentOrderService;
        }

        public static void SplitPaymentAcrossMainAndChildLoans(
            List<ICreditPaymentPlacementModel> credits,
            decimal paymentInitialAmount,
            List<PaymentOrderItem> paymentOrder,
            out decimal remainingUnplacedAmount)
        {
            //TODO: Settle
            //TODO: For auto place
            //TODO: Writing off small amounts
            //TODO: How to deal with repayment ... now we are putting it on one of the main credit
            var remainingAmount = paymentInitialAmount;

            //Notifications
            var notifications = credits.SelectMany(c => c.GetOpenNotifications().Select(n => new
            {
                Credit = c,
                Notification = n
            }))
                .OrderBy(x => x.Notification.DueDate)
                .ThenByDescending(x => x.Notification.GetTotalRemainingBalance())
                .ThenBy(x => x.Credit.IsMainCredit ? 0 : 1) //Main loan before child loans or solo loans
                .ThenBy(x => x.Notification.NotificationId)
                .ToList(); //Tie breaker that should never be needed

            //One notification at a time
            foreach (var n in notifications)
            {
                foreach (var amountType in paymentOrder)
                {
                    var balance = n.Notification.GetRemainingBalance(amountType);
                    var placedAmount = PlaceUpTo(balance, ref remainingAmount);
                    if (placedAmount > 0m)
                        n.Notification.PlacePayment(amountType, placedAmount);
                }
            }

            //Extra amortizations: Note, this only happens if all notifications have been paid so we can
            //                     use not notified capital as actual capital. This will not be true if we dont pay notifications first.
            var notNotifiedCapitalBalance = credits.Sum(x => x.GetNotNotifiedCapitalBalance());

            //First smear the payment out so all are paid at the same rate.
            var amountToSplit = remainingAmount;
            foreach (var credit in credits)
            {
                if (notNotifiedCapitalBalance <= 0 || remainingAmount <= 0m)
                    break;
                var placedAmount = PlaceUpTo(Math.Round(amountToSplit * credit.GetNotNotifiedCapitalBalance() / notNotifiedCapitalBalance, 2), ref remainingAmount);
                if (placedAmount > 0m)
                    credit.PlaceExtraAmortizationPayment(placedAmount);
            }

            //Deal with rounding or previous uneven payment rate and just place the remaining wherever possible
            foreach (var credit in credits)
            {
                if (remainingAmount <= 0m)
                    break;
                var placedAmount = PlaceUpTo(credit.GetNotNotifiedCapitalBalance(), ref remainingAmount);
                if (placedAmount > 0m)
                    credit.PlaceExtraAmortizationPayment(placedAmount);
            }

            remainingUnplacedAmount = remainingAmount;
        }

        //Just a helper to make it harder to desynch remainingAmount when counting it down
        private static decimal PlaceUpTo(decimal potentialAmount, ref decimal remainingAmount)
        {
            if (potentialAmount < 0m || remainingAmount < 0m)
                throw new Exception("Negative amount!");

            var placedAmount = Math.Min(potentialAmount, remainingAmount);
            if (placedAmount > 0m)
            {
                remainingAmount = remainingAmount - placedAmount;
                return placedAmount;
            }
            else
                return 0m;
        }

        ///<summary>
        /// Split payments across main/child credits
        ///</summary>
        public void SplitPaymentsInIncomingFileBySharedPaymentOcr(ICreditContextExtended context, IncomingPaymentFileWithOriginal paymentFile, PaymentPlacementBatchDataSource batchDataSource)
       {
            var q = context
                    .CreditHeadersQueryable
                    .Select(x => new
                    {
                        x.CreditNr,
                        OcrPaymentReference = x.DatedCreditStrings.Where(y => y.Name == DatedCreditStringCode.OcrPaymentReference.ToString()).OrderByDescending(y => y.BusinessEventId).Select(y => y.Value).FirstOrDefault(),
                        SharedOcrPaymentReference = x.DatedCreditStrings.Where(y => y.Name == DatedCreditStringCode.SharedOcrPaymentReference.ToString()).OrderByDescending(y => y.BusinessEventId).Select(y => y.Value).FirstOrDefault(),
                    })
                    .Where(x => x.SharedOcrPaymentReference != null)
                    .Select(x => new
                    {
                        x.CreditNr,
                        x.OcrPaymentReference,
                        x.SharedOcrPaymentReference
                    });

            //TODO: The split does currently ignore several payments in the same file
            Func<IncomingPaymentFile.AccountDateBatchPayment, decimal, string, string, IncomingPaymentFile.AccountDateBatchPayment> clone = (pi, amt, ocr, extraText) =>
            {
                return new IncomingPaymentFile.AccountDateBatchPayment
                {
                    Amount = amt,
                    OcrReference = ocr,
                    AutogiroPayerNumber = pi.AutogiroPayerNumber,
                    CustomerAccountNr = pi.CustomerAccountNr,
                    CustomerAddressBuildingNumber = pi.CustomerAddressBuildingNumber,
                    CustomerAddressCountry = pi.CustomerAddressCountry,
                    CustomerAddressLines = pi.CustomerAddressLines,
                    CustomerAddressPostalCode = pi.CustomerAddressPostalCode,
                    CustomerAddressStreetName = pi.CustomerAddressStreetName,
                    CustomerAddressTownName = pi.CustomerAddressTownName,
                    CustomerName = pi.CustomerName,
                    CustomerOrgnr = pi.CustomerOrgnr,
                    ExternalId = pi.ExternalId,
                    InformationText = string.IsNullOrWhiteSpace(pi.InformationText) ? extraText : (pi.InformationText + Environment.NewLine + extraText)
                };
            };

            //NOTE: This is slow since its sort of a beta ... can be made way faster by batching
            var payments = paymentFile.Accounts.SelectMany(a => a.DateBatches.SelectMany(db => db.Payments.SelectMany(p =>
            {
                var matchedCredits = q.Where(x => x.SharedOcrPaymentReference == p.OcrReference).ToList();

                if (matchedCredits.Count == 0)
                {
                    return Enumerables.Singleton(
                        new
                        {
                            a.AccountNr,
                            a.Currency,
                            db.BookKeepingDate,
                            Payment = p
                        });
                }

                var creditModels = CreditPaymentPlacementModel.LoadBatch(
                    matchedCredits.Select(x => x.CreditNr).ToHashSetShared(),
                    context, envSettings, ClientCfg, batchDataSource);

                var initialPaymentAmount = p.Amount;

                SplitPaymentAcrossMainAndChildLoans(creditModels.Values.Cast<ICreditPaymentPlacementModel>().ToList(), p.Amount, paymentOrderService.GetPaymentOrderItems(), out var unplacedAmount);

                return creditModels.Values.Select(x => x.Placements.Sum(y => y.Amount) > 0m ? new
                {
                    a.AccountNr,
                    a.Currency,
                    db.BookKeepingDate,
                    Payment = clone(p, x.Placements.Sum(y => y.Amount),
                    matchedCredits.Single(y => y.CreditNr == x.CreditNr).OcrPaymentReference,
                    $"Split from {initialPaymentAmount.ToString("F2", CommentFormattingCulture)} payment against {p.OcrReference}")
                } : null)
                .Concat(new[] { unplacedAmount <= 0m ? null  : new
                {
                    a.AccountNr,
                    a.Currency,
                    db.BookKeepingDate,
                    Payment = clone(p, unplacedAmount, p.OcrReference, $"Split from {initialPaymentAmount.ToString("F2", CommentFormattingCulture)} payment against {p.OcrReference}")
                }})
                .Where(x => x != null)
                .ToArray();
            })))
            .ToList();

            paymentFile.Accounts = payments
                .GroupBy(x => new { x.AccountNr, x.Currency })
                .Select(ag => new IncomingPaymentFile.Account
                {
                    AccountNr = ag.Key.AccountNr,
                    Currency = ag.Key.Currency,
                    DateBatches = ag
                        .GroupBy(y => y.BookKeepingDate)
                        .Select(bd => new IncomingPaymentFile.AccountDateBatch
                        {
                            BookKeepingDate = bd.Key,
                            Payments = bd.Select(z => z.Payment).ToList()
                        })
                        .ToList()
                }).ToList();
        }
    }
}