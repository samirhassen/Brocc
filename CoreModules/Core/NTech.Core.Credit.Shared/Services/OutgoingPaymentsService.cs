using NTech.Core.Credit.Shared.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.Code.Services
{
    public class OutgoingPaymentsService : IOutgoingPaymentsService
    {
        private readonly CreditContextFactory contextFactory;

        public OutgoingPaymentsService(CreditContextFactory contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public List<FetchPaymentsOutgoingPaymentModel> FetchPayments(int outgoingPaymentFileHeaderId)
        {
            using (var context = contextFactory.CreateContext())
            {
                var result = context
                    .OutgoingPaymentHeadersQueryable
                    .Where(x => x.OutgoingPaymentFileHeaderId == outgoingPaymentFileHeaderId)
                    .Select(x => new
                    {
                        x.Id,
                        EventTypePaymentSource = x.CreatedByEvent.EventType,
                        ItemApplicationProviderName = x
                            .Items
                            .Where(y => y.Name == OutgoingPaymentHeaderItemCode.ApplicationProviderName.ToString() && !y.IsEncrypted)
                            .OrderByDescending(y => y.Timestamp)
                            .Select(y => y.Value)
                            .FirstOrDefault(),
                        ItemProviderApplicationId = x
                            .Items
                            .Where(y => y.Name == OutgoingPaymentHeaderItemCode.ProviderApplicationId.ToString() && !y.IsEncrypted)
                            .OrderByDescending(y => y.Timestamp)
                            .Select(y => y.Value)
                            .FirstOrDefault(),
                        ItemApplicationNr = x
                            .Items
                            .Where(y => y.Name == OutgoingPaymentHeaderItemCode.ApplicationNr.ToString() && !y.IsEncrypted)
                            .OrderByDescending(y => y.Timestamp)
                            .Select(y => y.Value)
                            .FirstOrDefault(),
                        CreditNr = x
                            .Items
                            .Where(y => y.Name == OutgoingPaymentHeaderItemCode.CreditNr.ToString() && !y.IsEncrypted)
                            .OrderByDescending(y => y.Timestamp)
                            .Select(y => y.Value)
                            .FirstOrDefault(),
                        x.OutgoingPaymentFile.TransactionDate,
                        x.OutgoingPaymentFile.BookKeepingDate,
                        PaidToCustomerTransactions = x
                            .Transactions
                            .Where(y => y.BusinessEventId == x.OutgoingPaymentFile.CreatedByBusinessEventId
                                        && y.AccountCode == TransactionAccountType.ShouldBePaidToCustomer.ToString()
                                        && !y.WriteoffId.HasValue)
                    })
                    .Where(x => x.PaidToCustomerTransactions.Any())
                    .Select(x => new
                    {
                        x.Id,
                        x.EventTypePaymentSource,
                        x.TransactionDate,
                        x.BookKeepingDate,
                        PaidToCustomerAmount = -x.PaidToCustomerTransactions.Sum(y => y.Amount),
                        x.ItemApplicationNr,
                        x.ItemApplicationProviderName,
                        x.ItemProviderApplicationId,
                        x.CreditNr
                    })
                    .OrderBy(x => x.Id)
                    .ToList();

                var creditNrs = result.Select(x => x.CreditNr).Where(x => x != null).Distinct().ToList();

                var credits = context
                    .CreditHeadersQueryable
                    .Where(x => creditNrs.Contains(x.CreditNr))
                    .Select(x => new
                    {
                        x.CreditNr,
                        x.ProviderName,
                        ProviderApplicationId = x.DatedCreditStrings.Where(y => y.Name == DatedCreditStringCode.ProviderApplicationId.ToString()).OrderByDescending(y => y.Id).Select(y => y.Value).FirstOrDefault(),
                        ApplicationNr = x.DatedCreditStrings.Where(y => y.Name == DatedCreditStringCode.ApplicationNr.ToString()).OrderByDescending(y => y.Id).Select(y => y.Value).FirstOrDefault()
                    })
                    .ToList()
                    .ToDictionary(x => x.CreditNr, x => x);

                return result.Select(x =>
                {
                    var credit = x.CreditNr == null ? null : credits.Opt(x.CreditNr);
                    return new FetchPaymentsOutgoingPaymentModel
                    {
                        Id = x.Id,
                        EventTypePaymentSource = x.EventTypePaymentSource,
                        CreditNr = x.CreditNr,
                        CreditProviderName = x.ItemApplicationProviderName ?? credit?.ProviderName,
                        TransactionDate = x.TransactionDate,
                        BookKeepingDate = x.BookKeepingDate,
                        ApplicationNr = x.ItemApplicationNr ?? credit?.ApplicationNr,
                        ProviderApplicationId = x.ItemProviderApplicationId ?? credit?.ProviderApplicationId,
                        PaidToCustomerAmount = x.PaidToCustomerAmount
                    };
                }).ToList();
            }
        }
    }
}