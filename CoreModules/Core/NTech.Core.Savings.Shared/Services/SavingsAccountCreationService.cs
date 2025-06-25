using System;
using System.Collections.Generic;
using System.Linq;
using NTech.Banking.Conversion;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Core.Savings.Shared.BusinessEvents;
using NTech.Core.Savings.Shared.Database;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;

namespace NTech.Core.Savings.Shared.Services
{
    public class SavingsAccountCreationService
    {
        private readonly ICoreClock _clock;
        private readonly ILoggingService _loggingService;
        private readonly ICustomerClient _customerClient;

        private readonly Action<(string SavingsAccountNr, ISavingsContext Context, string SendingLocation)>
            _sendWelcomeEmail;

        private readonly Func<(SavingsAccountTypeCode Value, Guid? product, DateTime Today), bool> _hasInterestRateFor;
        private readonly CreateSavingsAccountBusinessEventManager _createAccountMgr;
        private readonly SavingsContextFactory _contextFactory;
        private readonly ICustomerRelationsMergeService _relationsMergeService;

        public SavingsAccountCreationService(
            ICoreClock clock, ILoggingService loggingService, ICustomerClient customerClient,
            Action<(string SavingsAccountNr, ISavingsContext Context, string SendingLocation)> sendWelcomeEmail,
            Func<(SavingsAccountTypeCode AccountType, Guid? Product, DateTime Date), bool> hasInterestRateFor,
            CreateSavingsAccountBusinessEventManager createAccountMgr, SavingsContextFactory contextFactory,
            ICustomerRelationsMergeService relationsMergeService)
        {
            _clock = clock;
            _loggingService = loggingService;
            _customerClient = customerClient;
            _sendWelcomeEmail = sendWelcomeEmail;
            _hasInterestRateFor = hasInterestRateFor;
            _createAccountMgr = createAccountMgr;
            _contextFactory = contextFactory;
            _relationsMergeService = relationsMergeService;
        }

        public CreateSavingsAccountResponse CreateAccount(CreateSavingsAccountRequest request)
        {
            var applicationItems = request?.ApplicationItems;
            var externalVariables = request?.ExternalVariables;
            var allowSavingsAccountNrGeneration = request?.AllowSavingsAccountNrGeneration;
            var allowCreateWithoutSignedAgreement = request?.AllowCreateWithoutSignedAgreement;

            if (applicationItems == null)
            {
                throw new NTechCoreWebserviceException("items missing")
                    { IsUserFacing = true, ErrorHttpStatusCode = 400 };
            }

            using (var context = _contextFactory.CreateContext())
            {
                var savingsAccountTypeCode = applicationItems
                    .FirstOrDefault(x => x.Name == nameof(SavingsApplicationItemName.savingsAccountTypeCode))?.Value;
                var savingsAccountTypeCodeP = Enums.Parse<SavingsAccountTypeCode>(savingsAccountTypeCode);
                if (!savingsAccountTypeCodeP.HasValue)
                    throw new NTechCoreWebserviceException("Missing or invalid savingsAccountTypeCode")
                        { IsUserFacing = true, ErrorHttpStatusCode = 400 };

                var hasProduct = TryGetProduct(applicationItems, out var product);
                if (savingsAccountTypeCodeP == SavingsAccountTypeCode.FixedInterestAccount && !hasProduct)
                    throw new NTechCoreWebserviceException("Missing or invalid fixedInterestProduct")
                        { IsUserFacing = true, ErrorHttpStatusCode = 400 };

                if (!_hasInterestRateFor((savingsAccountTypeCodeP.Value, product, _clock.Today)))
                {
                    throw new NTechCoreWebserviceException("Account type has no active interest rate")
                        { IsUserFacing = true, ErrorHttpStatusCode = 400 };
                }

                var creationOptions = new CreateSavingsAccountBusinessEventManager.CreationOptions
                {
                    AllowAccountNrGeneration = allowSavingsAccountNrGeneration.GetValueOrDefault(),
                    AllowCreateWithoutSignedAgreement = allowCreateWithoutSignedAgreement.GetValueOrDefault()
                };

                var isOk = _createAccountMgr.TryCreateSavingsAccount(
                    context,
                    applicationItems.Select(x => Tuple.Create(x.Name, x.Value)).ToList(),
                    externalVariables?.Select(x => Tuple.Create(x.Name, x.Value)).ToList(),
                    out var failedMessage,
                    out var savingsAccount,
                    out var ocrPaymentReference,
                    creationOptions: creationOptions);

                context.SaveChanges();

                if (!isOk)
                    throw new NTechCoreWebserviceException(failedMessage)
                    {
                        IsUserFacing = true, ErrorHttpStatusCode = 400
                    };

                try
                {
                    _customerClient.MergeCustomerRelations(new List<CustomerClientCustomerRelation>
                    {
                        new CustomerClientCustomerRelation
                        {
                            CustomerId = savingsAccount.MainCustomerId,
                            RelationId = savingsAccount.SavingsAccountNr,
                            RelationType = $"SavingsAccount_{savingsAccount.AccountTypeCode}",
                            StartDate = _clock.Today,
                            EndDate = null
                        }
                    });
                    _relationsMergeService.MergeSavingsAccountsToCustomerRelations(
                        onlySavingsAccountNrs: new HashSet<string> { savingsAccount.SavingsAccountNr });
                }
                catch (Exception ex)
                {
                    _loggingService.Error(ex,
                        $"Failed to merge savings account relation on new account {savingsAccount?.SavingsAccountNr}");
                }

                _sendWelcomeEmail((savingsAccount.SavingsAccountNr, context,
                    $"OnCreate_{savingsAccount.SavingsAccountNr}"));

                context.SaveChanges();

                return new CreateSavingsAccountResponse
                {
                    savingsAccountNr = savingsAccount.SavingsAccountNr,
                    status = savingsAccount.Status,
                    ocrPaymentReference = ocrPaymentReference.NormalForm
                };
            }
        }

        private static bool TryGetProduct(IList<CreateSavingsAccountItem> applicationItems, out Guid? productId)
        {
            productId = null;
            var product = applicationItems.SingleOrDefault(x =>
                x.Name == nameof(SavingsApplicationItemName.fixedInterestProduct));
            if (product == null || !Guid.TryParse(product.Value, out var pg)) return false;

            productId = pg;
            return true;
        }
    }

    public class CreateSavingsAccountRequest
    {
        public IList<CreateSavingsAccountItem> ApplicationItems { get; set; }
        public IList<CreateSavingsAccountItem> ExternalVariables { get; set; }
        public bool? AllowSavingsAccountNrGeneration { get; set; }
        public bool? AllowCreateWithoutSignedAgreement { get; set; }
    }

    public class CreateSavingsAccountItem
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class CreateSavingsAccountResponse
    {
        public string savingsAccountNr { get; set; }
        public string status { get; set; }
        public string ocrPaymentReference { get; set; }
    }
}