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

namespace nCredit.DbModel.BusinessEvents.NewCredit
{
    public class NewCreditOutgoingPaymentService : BusinessEventManagerOrServiceBase
    {
        private readonly BankAccountNumberParser bankAccountNumberParser;
        private readonly EncryptionService encryptionService;
        private readonly ICreditEnvSettings envSettings;
        private readonly ICustomerClient customerClient;
        private readonly PaymentAccountService paymentAccountService;

        public NewCreditOutgoingPaymentService(INTechCurrentUserMetadata currentUser, BankAccountNumberParser bankAccountNumberParser, EncryptionService encryptionService,
            IClientConfigurationCore clientConfiguration, ICoreClock coreClock, ICreditEnvSettings envSettings, ICustomerClient customerClient, PaymentAccountService paymentAccountService) : base(currentUser, coreClock, clientConfiguration)
        {
            this.bankAccountNumberParser = bankAccountNumberParser;
            this.encryptionService = encryptionService;
            this.envSettings = envSettings;
            this.customerClient = customerClient;
            this.paymentAccountService = paymentAccountService;
        }

        public (List<OutgoingPaymentHeader> Payments, decimal PaidToCustomerAmount, Action SaveEncryptedItems) CreateSplitOutgoingPayment(NewCreditRequest request, ICreditContextExtended context, DateTime bookKeepingDate, BusinessEvent newCreditEvent, CreditHeader credit)
        {
            bool isCompanyCredit = request.IsCompanyCredit ?? false;

            context.EnsureCurrentTransaction();

            if (request.CreditAmount > 0m)
                throw new Exception("This method does not support using CreditAmount. Use CreateSingleOutgoingPayment instead");

            var payments = new List<OutgoingPaymentHeader>();
            var saves = new List<Action>();
            var paidToCustomerAmount = 0m;

            foreach (var part in request.CreditAmountParts.Where(x => x.ShouldBePaidOut))
            {
                if (part.IsCoveringInitialFeeDrawnFromLoan.GetValueOrDefault())
                    throw new Exception("Capital covering initial fee withheld cannot be paid out");
                var toBankAccountNr = bankAccountNumberParser.ParseFromStringWithDefaults(part.PaymentBankAccountNr, part.PaymentBankAccountNrType);

                var paymentInfo = new SinglePaymentInfo
                {
                    BookKeepingDate = bookKeepingDate,
                    CreditAmount = part.Amount,
                    DrawnFromLoanAmountInitialFeeAmount = 0m,
                    ToBankAccountNr = toBankAccountNr,
                    PaymentReference = part.PaymentReference,
                    PaymentMessage = part.PaymentMessage
                };

                var result = CreateSingleOutgoingPaymentInternal(request, isCompanyCredit, context, newCreditEvent, credit,
                    paymentInfo, part.SubAccountCode);

                payments.Add(result.Payment);
                saves.Add(result.SaveEncryptedItems);
                paidToCustomerAmount += result.PaidToCustomerAmount;
            }

            Action SaveEncryptedItems = () =>
            {
                foreach (var save in saves)
                    save();
            };

            return (Payments: payments, PaidToCustomerAmount: paidToCustomerAmount, SaveEncryptedItems: SaveEncryptedItems);
        }

        public (OutgoingPaymentHeader Payment, decimal PaidToCustomerAmount, Action SaveEncryptedItems) CreateSingleOutgoingPayment(NewCreditRequest request, ICreditContextExtended context, DateTime bookKeepingDate, BusinessEvent newCreditEvent, CreditHeader credit, IBankAccountNumber toBankAccountNr)
        {
            bool isCompanyCredit = request.IsCompanyCredit ?? false;

            context.EnsureCurrentTransaction();

            if (request.HasCreditAmountParts)
                throw new Exception("This method does not support multiple payments. Use CreateSplitOutgoingPayments instead");

            var paymentInfo = new SinglePaymentInfo
            {
                BookKeepingDate = bookKeepingDate,
                CreditAmount = request.CreditAmount,
                DrawnFromLoanAmountInitialFeeAmount = request.DrawnFromLoanAmountInitialFeeAmount ?? 0m,
                ToBankAccountNr = toBankAccountNr
            };

            return CreateSingleOutgoingPaymentInternal(request, isCompanyCredit, context, newCreditEvent, credit, paymentInfo, null);
        }

        public IBankAccountNumber HandleOutgoingPaymentBankAccount(NewCreditRequest request, ICreditContextExtended context, BusinessEvent newCreditEvent, CreditHeader credit)
        {
            var accountNrString = request.Iban ?? request.BankAccountNr;

            if (string.IsNullOrWhiteSpace(accountNrString) && (request.IsInitialPaymentAlreadyMade.GetValueOrDefault() || request.CreditAmountParts != null))
                return null; //We allow null here with CreditAmountParts since the request level account is only informational in that case. The actual payment accounts are per part.

            var toBankAccountNr = bankAccountNumberParser.ParseFromStringWithDefaults(accountNrString, request.BankAccountNrType);
            if (toBankAccountNr == null)
                return null;

            if (ClientCfg.Country.BaseCountry == "SE")
            {
                AddDatedCreditString(DatedCreditStringCode.BankAccountNr.ToString(), toBankAccountNr.FormatFor(null), credit, newCreditEvent, context);
            }
            else if (ClientCfg.Country.BaseCountry == "FI")
            {
                AddDatedCreditString(DatedCreditStringCode.Iban.ToString(), toBankAccountNr.FormatFor(null), credit, newCreditEvent, context);
            }
            else
                throw new NotImplementedException();

            AddDatedCreditString(DatedCreditStringCode.BankAccountNrType.ToString(), toBankAccountNr.AccountType.ToString(), credit, newCreditEvent, context);

            return toBankAccountNr;
        }

        private (OutgoingPaymentHeader Payment, decimal PaidToCustomerAmount, Action SaveEncryptedItems) CreateSingleOutgoingPaymentInternal(
            NewCreditRequestExceptCapital request, bool isCompanyCredit, ICreditContextExtended context, BusinessEvent newCreditEvent, CreditHeader credit,
            SinglePaymentInfo paymentInfo,
            string subAccountCode)
        {
            context.EnsureCurrentTransaction();

            var paidToCustomerAmount = 0m;

            OutgoingPaymentHeader outgoingPayment = new OutgoingPaymentHeader
            {
                BookKeepingDate = newCreditEvent.BookKeepingDate,
                ChangedById = newCreditEvent.ChangedById,
                ChangedDate = newCreditEvent.ChangedDate,
                InformationMetaData = newCreditEvent.InformationMetaData,
                TransactionDate = newCreditEvent.TransactionDate,
                CreatedByEvent = newCreditEvent
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

            var message = paymentInfo.PaymentMessage ?? GetOutgoingPaymentFileCustomerMessage(envSettings, eventName: "Initial payment", contextNumber: credit.CreditNr);
            addItem(OutgoingPaymentHeaderItemCode.CustomerMessage, message, false);

            string applicant1CustomerName;
            if (!isCompanyCredit)
                applicant1CustomerName = GetApplicant1CustomerNameFromCustomerCard(request.Applicants);
            else
                applicant1CustomerName = GetCompanyName(request.Applicants.Single().CustomerId);

            addItem(OutgoingPaymentHeaderItemCode.CustomerName, applicant1CustomerName, true);
            addItem(OutgoingPaymentHeaderItemCode.CreditNr, credit.CreditNr, false);

            if (!string.IsNullOrWhiteSpace(request.ProviderName))
                addItem(OutgoingPaymentHeaderItemCode.ApplicationProviderName, request.ProviderName, false);

            if (!string.IsNullOrWhiteSpace(request.ProviderApplicationId))
                addItem(OutgoingPaymentHeaderItemCode.ProviderApplicationId, request.ProviderApplicationId, false);

            if (!string.IsNullOrWhiteSpace(request.ApplicationNr))
                addItem(OutgoingPaymentHeaderItemCode.ApplicationNr, request.ApplicationNr, false);

            if (!string.IsNullOrWhiteSpace(paymentInfo.PaymentReference))
                addItem(OutgoingPaymentHeaderItemCode.PaymentReference, paymentInfo.PaymentReference, false);

            var outgoingPaymentSourceAccount = paymentAccountService.GetOutgoingPaymentSourceBankAccountNr();
            if (ClientCfg.Country.BaseCountry == "SE")
            {
                addItem(OutgoingPaymentHeaderItemCode.FromBankAccountNr, outgoingPaymentSourceAccount.FormatFor(null), false);
                addItem(OutgoingPaymentHeaderItemCode.ToBankAccountNr, paymentInfo.ToBankAccountNr.FormatFor(null), false);

            }
            else if (ClientCfg.Country.BaseCountry == "FI")
            {
                addItem(OutgoingPaymentHeaderItemCode.FromIban, outgoingPaymentSourceAccount.FormatFor(null), false);
                addItem(OutgoingPaymentHeaderItemCode.ToIban, paymentInfo.ToBankAccountNr.FormatFor(null), false);
            }
            else
                throw new NotImplementedException();

            addItem(OutgoingPaymentHeaderItemCode.ToBankAccountNrType, paymentInfo.ToBankAccountNr.AccountType.ToString(), false);

            if (paymentInfo.CreditAmount > 0m)
            {
                context.AddAccountTransactions(CreateTransaction(
                    TransactionAccountType.ShouldBePaidToCustomer,
                    paymentInfo.CreditAmount,
                    paymentInfo.BookKeepingDate,
                    newCreditEvent,
                    credit: credit,
                    outgoingPayment: outgoingPayment,
                    businessEventRuleCode: "initialLoan",
                    subAccountCode: subAccountCode));

                paidToCustomerAmount += paymentInfo.CreditAmount;
            }
            if (paymentInfo.DrawnFromLoanAmountInitialFeeAmount > 0m
                && paymentInfo.DrawnFromLoanAmountInitialFeeAmount <= paymentInfo.CreditAmount)
            {
                context.AddAccountTransactions(CreateTransaction(
                    TransactionAccountType.ShouldBePaidToCustomer,
                    -paymentInfo.DrawnFromLoanAmountInitialFeeAmount,
                    paymentInfo.BookKeepingDate,
                    newCreditEvent,
                    credit: credit,
                    outgoingPayment: outgoingPayment,
                    businessEventRuleCode: "initialFee",
                    subAccountCode: subAccountCode));

                paidToCustomerAmount -= paymentInfo.DrawnFromLoanAmountInitialFeeAmount;
            }

            void SaveEncryptedPaymentHeaderItems()
            {
                var itemsToEncrypt = outgoingPayment.Items.Where(x => x.IsEncrypted == true).ToArray();
                if (itemsToEncrypt.Length > 0)
                {
                    encryptionService.SaveEncryptItems(itemsToEncrypt, x => x.Value,
                        (x, id) => x.Value = id.ToString(), context);
                }
            }

            return (Payment: outgoingPayment, PaidToCustomerAmount: paidToCustomerAmount, SaveEncryptedItems: () => SaveEncryptedPaymentHeaderItems());
        }

        public static string GetOutgoingPaymentFileCustomerMessage(ICreditEnvSettings envSettings, string eventName = null, string contextNumber = null)
        {
            var pattern = envSettings.OutgoingPaymentFileCustomerMessagePattern;

            pattern = pattern.Replace("{eventName}", eventName ?? "");
            pattern = pattern.Replace("{contextNumber}", contextNumber ?? "");

            if (string.IsNullOrWhiteSpace(pattern))
                return null;
            else
                return pattern.Trim();
        }

        private string GetCustomerNameFromCustomerCard(int customerId)
        {
            var customerCard = GetCustomerCardItems(customerClient, customerId, "firstName", "lastName");
            if (!customerCard.ContainsKey("firstName") || !customerCard.ContainsKey("lastName"))
            {
                throw new Exception("First name and or last name missing from customer card on customer: " + customerId);
            }
            return $"{customerCard["firstName"]} {customerCard["lastName"]}";
        }

        private string GetApplicant1CustomerNameFromCustomerCard(List<NewCreditRequest.Applicant> applicants)
        {
            var applicant1 = applicants.Single(x => x.ApplicantNr == 1);
            return GetCustomerNameFromCustomerCard(applicant1.CustomerId);
        }

        private string GetCompanyName(int customerId)
        {
            var customerCard = GetCustomerCardItems(customerClient, customerId, "companyName");
            if (!customerCard.ContainsKey("companyName"))
                throw new Exception("Company name missing from customer card on customer: " + customerId);

            return customerCard.OptVal("companyName");
        }

        private IDictionary<string, string> GetCustomerCardItems(ICustomerClient customerClient, int customerId, params string[] names) =>
            customerClient.BulkFetchPropertiesByCustomerIdsD(new HashSet<int> { customerId }, names).OptVal(customerId);
    }

    public class SinglePaymentInfo
    {
        public decimal CreditAmount { get; set; }
        public decimal DrawnFromLoanAmountInitialFeeAmount { get; set; }
        public IBankAccountNumber ToBankAccountNr { get; set; }
        public DateTime BookKeepingDate { get; set; }
        public string PaymentReference { get; set; }
        public string PaymentMessage { get; set; }
    }
}