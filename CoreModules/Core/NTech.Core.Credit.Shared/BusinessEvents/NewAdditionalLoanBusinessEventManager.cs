using nCredit.Code.Services;
using nCredit.DbModel.BusinessEvents.NewCredit;
using nCredit.DbModel.Repository;
using NTech.Banking.BankAccounts;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.DbModel.BusinessEvents
{
    public class NewAdditionalLoanBusinessEventManager : BusinessEventManagerOrServiceBase
    {
        private readonly LegalInterestCeilingService legalInterestCeilingService;
        private readonly ICreditEnvSettings envSettings;
        private readonly ICustomerClient customerClient;
        private readonly EncryptionService encryptionService;
        private readonly PaymentAccountService paymentAccountService;

        public NewAdditionalLoanBusinessEventManager(INTechCurrentUserMetadata currentUser, LegalInterestCeilingService legalInterestCeilingService,
            ICreditEnvSettings envSettings, ICustomerClient customerClient, ICoreClock clock, IClientConfigurationCore clientConfiguration,
            EncryptionService encryptionService, PaymentAccountService paymentAccountService)
            : base(currentUser, clock, clientConfiguration)
        {
            this.legalInterestCeilingService = legalInterestCeilingService;
            this.envSettings = envSettings;
            this.customerClient = customerClient;
            this.encryptionService = encryptionService;
            this.paymentAccountService = paymentAccountService;
        }

        private string GetCustomerNameFromCustomerCard(int customerId)
        {
            var customerCard = customerClient.BulkFetchPropertiesByCustomerIdsD(new HashSet<int> { customerId }, "firstName", "lastName").Opt(customerId);
            if (!customerCard.ContainsKey("firstName") || !customerCard.ContainsKey("lastName"))
            {
                throw new Exception("First name and or last name missing from customer card on customer: " + customerId);
            }
            return $"{customerCard["firstName"]} {customerCard["lastName"]}";
        }

        private IBankAccountNumber ParseBankAccount(NewAdditionalLoanRequest r)
        {
            return new BankAccountNumberParser(ClientCfg.Country.BaseCountry).ParseFromStringWithDefaults(r.Iban ?? r.BankAccountNr, r.BankAccountNrType);
        }

        public BusinessEvent CreateNew(ICreditContextExtended context, NewAdditionalLoanRequest request)
        {
            context.EnsureCurrentTransaction();

            var bookKeepingDate = Now.ToLocalTime().Date;

            if (string.IsNullOrWhiteSpace(request.BankAccountNr) && string.IsNullOrWhiteSpace(request.Iban))
                throw new Exception($"{request.ApplicationNr}: Missing bank account/iban");
            if (string.IsNullOrWhiteSpace(request.CreditNr))
                throw new Exception($"{request.ApplicationNr}: Missing credit nr");
            if (!request.AdditionalLoanAmount.HasValue)
                throw new Exception($"{request.ApplicationNr}: Missing AdditionalLoanAmount");

            var additionalCommentText = "";

            var evt = new BusinessEvent
            {
                EventDate = Now,
                EventType = BusinessEventType.NewAdditionalLoan.ToString(),
                BookKeepingDate = bookKeepingDate,
                TransactionDate = Now.ToLocalTime().Date,
                ChangedById = UserId,
                ChangedDate = Now,
                InformationMetaData = InformationMetadata,
            };
            context.AddBusinessEvent(evt);

            var creditResult = context.CreditHeadersQueryable.Select(x => new { Credit = x, x.CreditCustomers }).Single(x => x.Credit.CreditNr == request.CreditNr);
            var credit = creditResult.Credit;

            if (credit.Status != CreditStatus.Normal.ToString())
                throw new Exception($"Application {request.ApplicationNr} is an additional loan but the credit {request.CreditNr} it applies to is not active");

            var creditData = new PartialCreditModelRepository().NewQuery(Clock.Today)
                .WithValues(DatedCreditValueCode.MarginInterestRate, DatedCreditValueCode.ReferenceInterestRate, DatedCreditValueCode.RequestedMarginInterestRate)
                .Execute(context, x => x.Where(y => y.Credit.CreditNr == request.CreditNr))
                .Single();

            if (request.NewAnnuityAmount.HasValue)
            {
                AddDatedCreditValue(DatedCreditValueCode.AnnuityAmount.ToString(), request.NewAnnuityAmount.Value, credit, evt, context);
                additionalCommentText += $", New Annuity Amount={request.NewAnnuityAmount.Value.ToString("f2", CommentFormattingCulture)}";
            }

            Func<decimal?, string, decimal> require = (v, txt) =>
            {
                if (!v.HasValue)
                    throw new Exception($"Missing required value {txt}");
                return v.Value;
            };

            var currentMarginInterestRate = require(creditData.GetValue(DatedCreditValueCode.MarginInterestRate), "MarginInterestRate");
            var interestChange = this.legalInterestCeilingService.HandleMarginInterestRateChange(
                creditData.GetValue(DatedCreditValueCode.ReferenceInterestRate) ?? 0m,
                creditData.GetValue(DatedCreditValueCode.RequestedMarginInterestRate),
                currentMarginInterestRate,
                request.NewMarginInterestRatePercent ?? currentMarginInterestRate);

            if (interestChange.NewMarginInterestRate.HasValue)
            {
                AddDatedCreditValue(DatedCreditValueCode.MarginInterestRate.ToString(), interestChange.NewMarginInterestRate.Value, credit, evt, context);
                additionalCommentText += $", New Margin Interest={(interestChange.NewMarginInterestRate.Value / 100m).ToString("P", CommentFormattingCulture)}";
            }
            if (interestChange.NewRequestedMarginInterestRate.HasValue)
            {
                AddDatedCreditValue(DatedCreditValueCode.RequestedMarginInterestRate.ToString(), interestChange.NewRequestedMarginInterestRate.Value, credit, evt, context);
                additionalCommentText += $", New Requested Margin Interest={(interestChange.NewRequestedMarginInterestRate.Value / 100m).ToString("P", CommentFormattingCulture)}";
            }

            if (request.NewNotificationFeeAmount.HasValue)
            {
                AddDatedCreditValue(DatedCreditValueCode.NotificationFee.ToString(), request.NewNotificationFeeAmount.Value, credit, evt, context);
                additionalCommentText += $", New Notification Fee={request.NewNotificationFeeAmount.Value.ToString("f2", CommentFormattingCulture)}";
            }
            if (!string.IsNullOrWhiteSpace(request.CampaignCode))
            {
                additionalCommentText += $", Campaign code={request.CampaignCode}";
            }

            var toBankAccount = ParseBankAccount(request);

            if (ClientCfg.Country.BaseCountry == "SE")
            {
                AddDatedCreditString(DatedCreditStringCode.BankAccountNr.ToString(), toBankAccount.FormatFor(null), credit, evt, context);
            }
            else if (ClientCfg.Country.BaseCountry == "FI")
            {
                AddDatedCreditString(DatedCreditStringCode.Iban.ToString(), toBankAccount.FormatFor(null), credit, evt, context);
            }
            else
                throw new NotImplementedException();

            AddDatedCreditString(DatedCreditStringCode.BankAccountNrType.ToString(), toBankAccount.AccountType.ToString(), credit, evt, context);

            var commentArchiveKeys = new List<string>();
            if (request.Agreements != null)
            {
                //Reorder these in case the application has the customers in reverse order to the credit
                foreach (var c in creditResult.CreditCustomers.OrderBy(x => x.ApplicantNr))
                {
                    var a = request.Agreements.SingleOrDefault(x => x.CustomerId == c.CustomerId);
                    if (a != null && a.AgreementPdfArchiveKey != null)
                    {
                        AddCreditDocument("AdditionalLoanAgreement", c.ApplicantNr, a.AgreementPdfArchiveKey, context, credit: credit);
                        commentArchiveKeys.Add(a.AgreementPdfArchiveKey);
                    }
                }
            }

            context.AddAccountTransactions(CreateTransaction(
                TransactionAccountType.NotNotifiedCapital,
                request.AdditionalLoanAmount.Value,
                bookKeepingDate,
                evt,
                credit: credit));

            //Create a payment to the customer
            var paidToCustomerAmount = request.AdditionalLoanAmount.Value; //NOTE: If this ever separates from credit amount
            OutgoingPaymentHeader outgoingPayment = null;
            if (paidToCustomerAmount > 0m)
            {
                outgoingPayment = new OutgoingPaymentHeader
                {
                    BookKeepingDate = evt.BookKeepingDate,
                    ChangedById = evt.ChangedById,
                    ChangedDate = evt.ChangedDate,
                    InformationMetaData = evt.InformationMetaData,
                    TransactionDate = evt.TransactionDate,
                    CreatedByEvent = evt
                };
                context.AddOutgoingPaymentHeader(outgoingPayment);

                Action<OutgoingPaymentHeaderItemCode, string, bool> addItem = (name, value, isEncrypted) =>
                    {
                        var item = new OutgoingPaymentHeaderItem
                        {
                            ChangedById = UserId,
                            ChangedDate = Clock.Now,
                            IsEncrypted = isEncrypted,
                            InformationMetaData = InformationMetadata,
                            OutgoingPayment = outgoingPayment,
                            Name = name.ToString(),
                            Value = value
                        };
                        context.AddOutgoingPaymentHeaderItem(item);
                    };

                var applicant1CustomerName = GetCustomerNameFromCustomerCard(creditResult.CreditCustomers.Single(x => x.ApplicantNr == 1).CustomerId);
                addItem(OutgoingPaymentHeaderItemCode.CustomerMessage, NewCreditOutgoingPaymentService.GetOutgoingPaymentFileCustomerMessage(envSettings, eventName: "Additional loan", contextNumber: credit.CreditNr), false);
                addItem(OutgoingPaymentHeaderItemCode.CustomerName, applicant1CustomerName, true);
                addItem(OutgoingPaymentHeaderItemCode.CreditNr, credit.CreditNr, false);

                if (!string.IsNullOrWhiteSpace(request.ProviderName))
                {
                    addItem(OutgoingPaymentHeaderItemCode.ApplicationProviderName, request.ProviderName, false);
                }
                if (!string.IsNullOrWhiteSpace(request.ProviderApplicationId))
                {
                    addItem(OutgoingPaymentHeaderItemCode.ProviderApplicationId, request.ProviderApplicationId, false);
                }
                if (!string.IsNullOrWhiteSpace(request.ApplicationNr))
                {
                    addItem(OutgoingPaymentHeaderItemCode.ApplicationNr, request.ApplicationNr, false);
                }

                var outgoingPaymentSourceAccount = paymentAccountService.GetOutgoingPaymentSourceBankAccountNr();
                if (ClientCfg.Country.BaseCountry == "SE")
                {
                    addItem(OutgoingPaymentHeaderItemCode.FromBankAccountNr, outgoingPaymentSourceAccount.FormatFor(null), false);
                    addItem(OutgoingPaymentHeaderItemCode.ToBankAccountNr, toBankAccount.FormatFor(null), false);
                }
                else if (ClientCfg.Country.BaseCountry == "FI")
                {
                    addItem(OutgoingPaymentHeaderItemCode.FromIban, outgoingPaymentSourceAccount.FormatFor(null), false);
                    addItem(OutgoingPaymentHeaderItemCode.ToIban, toBankAccount.FormatFor(null), false);
                }
                else
                    throw new NotImplementedException();

                addItem(OutgoingPaymentHeaderItemCode.ToBankAccountNrType, toBankAccount.AccountType.ToString(), false);

                context.AddAccountTransactions(CreateTransaction(
                    TransactionAccountType.ShouldBePaidToCustomer,
                    paidToCustomerAmount,
                    bookKeepingDate,
                    evt,
                    credit: credit,
                    outgoingPayment: outgoingPayment));
            }

            context.AddAccountTransactions(CreateTransaction(
                TransactionAccountType.CapitalDebt,
                request.AdditionalLoanAmount.Value,
                bookKeepingDate,
                evt,
                credit: credit));

            AddComment(
                $"Additional loan: {paidToCustomerAmount.ToString("f2", CommentFormattingCulture)} will be paid to the customer. Source application={request.ApplicationNr}, Application provider={request.ProviderName}{additionalCommentText}",
                BusinessEventType.NewAdditionalLoan,
                context,
                credit: credit,
                attachment: CreditCommentAttachmentModel.ArchiveKeysOnly(commentArchiveKeys));

            ////////////////////////////////////////////////
            //////////////// Handle encryption /////////////
            ////////////////////////////////////////////////
            if (outgoingPayment != null)
            {
                var itemsToEncrypt = outgoingPayment.Items.Where(x => x.IsEncrypted == true).ToArray();
                if (itemsToEncrypt.Length > 0)
                {
                    context.EnsureCurrentTransaction();

                    encryptionService.SaveEncryptItems(
                        itemsToEncrypt,
                        x => x.Value,
                        (x, id) => x.Value = id.ToString(),
                        context);
                }
            }

            return evt;
        }
    }
}