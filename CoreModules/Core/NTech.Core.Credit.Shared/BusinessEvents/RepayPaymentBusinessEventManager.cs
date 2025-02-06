using nCredit.DbModel.BusinessEvents.NewCredit;
using nCredit.DomainModel;
using NTech.Banking.BankAccounts;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nCredit.DbModel.BusinessEvents
{
    public class RepayPaymentBusinessEventManager : BusinessEventManagerOrServiceBase
    {
        private readonly EncryptionService encryptionService;
        private readonly ICreditEnvSettings envSettings;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly PaymentAccountService paymentAccountService;
        private readonly CreditContextFactory contextFactory;

        public RepayPaymentBusinessEventManager(INTechCurrentUserMetadata currentUser, EncryptionService encryptionService, ICreditEnvSettings envSettings,
            IClientConfigurationCore clientConfiguration, ICoreClock clock, PaymentAccountService paymentAccountService, CreditContextFactory contextFactory) : base(currentUser, clock, clientConfiguration)
        {
            this.encryptionService = encryptionService;
            this.envSettings = envSettings;
            this.clientConfiguration = clientConfiguration;
            this.paymentAccountService = paymentAccountService;
            this.contextFactory = contextFactory;
        }

        public bool TryRepay(
                int paymentId,
                decimal repaymentAmount,
                decimal leaveUnplacedAmount,
                string customerName,
                IBankAccountNumber toAccount,
                ICreditContextExtended context,
                out OutgoingPaymentHeader outgoingPayment,
                out string failedMessage)
        {
            context.EnsureCurrentTransaction();
            repaymentAmount = Math.Round(repaymentAmount, 2);
            leaveUnplacedAmount = Math.Round(leaveUnplacedAmount, 2);

            outgoingPayment = null;

            if (repaymentAmount <= 0m)
            {
                failedMessage = "repaymentAmount must be > 0";
                return false;
            }

            if (string.IsNullOrWhiteSpace(customerName))
            {
                failedMessage = "customerName is required";
                return false;
            }

            if (leaveUnplacedAmount < 0m)
            {
                failedMessage = "leaveUnplacedAmount negative";
                return false;
            }

            if (paymentId <= 0)
            {
                failedMessage = "Missing paymentId";
                return false;
            }

            var today = Clock.Today;

            PaymentDomainModel payment;
            payment = PaymentDomainModel.CreateForSinglePayment(paymentId, context, encryptionService);

            var unplacedAmount = payment.GetUnplacedAmount(today);

            var balanceAfter = unplacedAmount - repaymentAmount;

            if (balanceAfter < 0m || balanceAfter != leaveUnplacedAmount)
            {
                failedMessage = "Invalid repayment amount. Payment has balance: " + unplacedAmount.ToString(CommentFormattingCulture);
                return false;
            }

            var evt = new BusinessEvent
            {
                BookKeepingDate = Clock.Today, //One could argue this should inherit the payments bookkeeping-date                
                ChangedById = UserId,
                ChangedDate = Clock.Now,
                EventType = BusinessEventType.Repayment.ToString(),
                InformationMetaData = InformationMetadata,
                TransactionDate = Clock.Today,
                EventDate = Clock.Now
            };
            context.AddBusinessEvent(evt);

            var r = new OutgoingPaymentHeader
            {
                BookKeepingDate = evt.BookKeepingDate,
                ChangedById = evt.ChangedById,
                ChangedDate = evt.ChangedDate,
                InformationMetaData = evt.InformationMetaData,
                CreatedByEvent = evt,
                TransactionDate = evt.TransactionDate
            };
            context.AddOutgoingPaymentHeader(r);

            Action<OutgoingPaymentHeaderItemCode, string, bool> addItem = (name, value, isEncrypted) =>
            {
                var item = new OutgoingPaymentHeaderItem
                {
                    ChangedById = UserId,
                    ChangedDate = Clock.Now,
                    IsEncrypted = isEncrypted,
                    InformationMetaData = InformationMetadata,
                    OutgoingPayment = r,
                    Name = name.ToString(),
                    Value = value
                };
                if (r.Items == null)
                    r.Items = new List<OutgoingPaymentHeaderItem>();
                r.Items.Add(item);
                context.AddOutgoingPaymentHeaderItem(item);
            };
            addItem(OutgoingPaymentHeaderItemCode.CustomerMessage, NewCreditOutgoingPaymentService.GetOutgoingPaymentFileCustomerMessage(envSettings, eventName: "Repayment"), false);
            addItem(OutgoingPaymentHeaderItemCode.CustomerName, customerName, true);

            var c = clientConfiguration.Country.BaseCountry;
            var outgoingPaymentSourceAccount = paymentAccountService.GetOutgoingPaymentSourceBankAccountNr();
            if (c == "FI")
            {
                if (toAccount.AccountType != BankAccountNumberTypeCode.IBANFi)
                    throw new Exception($"Country FI does not support account type: {toAccount.AccountType}");
                addItem(OutgoingPaymentHeaderItemCode.FromIban, outgoingPaymentSourceAccount.FormatFor(null), false);
                addItem(OutgoingPaymentHeaderItemCode.ToIban, toAccount.FormatFor(null), false);
            }
            else if (c == "SE")
            {
                if (!(toAccount.AccountType == BankAccountNumberTypeCode.BankAccountSe || toAccount.AccountType == BankAccountNumberTypeCode.BankGiroSe || toAccount.AccountType == BankAccountNumberTypeCode.PlusGiroSe))
                    throw new Exception($"Country SE does not support account type: {toAccount.AccountType}");
                addItem(OutgoingPaymentHeaderItemCode.FromBankAccountNr, outgoingPaymentSourceAccount.FormatFor(null), false);
                addItem(OutgoingPaymentHeaderItemCode.ToBankAccountNr, toAccount.FormatFor(null), false);
            }
            else
                throw new NotImplementedException();

            addItem(OutgoingPaymentHeaderItemCode.ToBankAccountNrType, toAccount.AccountType.ToString(), false);

            List<AccountTransaction> trs = new List<AccountTransaction>();

            trs.Add(CreateTransaction(TransactionAccountType.UnplacedPayment, -repaymentAmount, evt.BookKeepingDate, evt, incomingPaymentId: paymentId));
            trs.Add(CreateTransaction(TransactionAccountType.ShouldBePaidToCustomer, repaymentAmount, evt.BookKeepingDate, evt, outgoingPayment: r));

            if (balanceAfter == 0m)
            {
                var p = context.IncomingPaymentHeadersQueryable.Single(x => x.Id == paymentId);
                p.IsFullyPlaced = true;
            }

            trs.ForEach(x => context.AddAccountTransactions(x));

            var itemsToEncrypt = r.Items.Where(x => x.IsEncrypted == true).ToArray();
            if (itemsToEncrypt.Length > 0)
            {
                encryptionService.SaveEncryptItems(
                    itemsToEncrypt,
                    x => x.Value,
                    (x, id) => x.Value = id.ToString(),
                    context);
            }

            failedMessage = null;
            outgoingPayment = r;
            return true;
        }

        public UnplacedCreditRepaymentResponse RepayPayment(UnplacedCreditRepaymentRequest request)
        {
            using (var context = contextFactory.CreateContext())
            {
                context.BeginTransaction();
                try
                {
                    string msg;
                    OutgoingPaymentHeader h;

                    var parser = new BankAccountNumberParser(clientConfiguration.Country.BaseCountry);

                    if (!TryRepay(request.PaymentId, request.RepaymentAmount, request.LeaveUnplacedAmount, request.CustomerName,
                        parser.ParseFromStringWithDefaults(request.BankAccountNr, request.BankAccountNrType),
                        context, out h, out msg))
                    {
                        throw new NTechCoreWebserviceException(msg) { IsUserFacing = true, ErrorHttpStatusCode = 400 };
                    }
                    else
                    {
                        context.SaveChanges();
                        context.CommitTransaction();

                        return new UnplacedCreditRepaymentResponse { };
                    }
                }
                catch
                {
                    context.RollbackTransaction();
                    throw;
                }
            }
        }
    }
}

public class UnplacedCreditRepaymentRequest
{
    [Required]
    public int PaymentId { get; set; }
    public string CustomerName { get; set; }
    [Required]
    public decimal RepaymentAmount { get; set; }
    [Required]
    public decimal LeaveUnplacedAmount { get; set; }    
    public string BankAccountNrType { get; set; }
    [Required]
    public string BankAccountNr { get; set; }
}

public class UnplacedCreditRepaymentResponse
{

}