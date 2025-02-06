using nSavings.Code;
using nSavings.Code.Services;
using nSavings.DbModel.BusinessEvents;
using NTech.Banking.Conversion;
using NTech.Core;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Core.Savings.Shared.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nSavings.Controllers
{
    public class SavingsAccountCreationService
    {
        private readonly ICoreClock clock;
        private readonly ILoggingService loggingService;
        private readonly ICustomerClient customerClient;
        private readonly Action<(string SavingsAccountNr, ISavingsContext Context, string SendingLocation)> sendWelcomeEmail;
        private readonly Func<(SavingsAccountTypeCode AccountType, DateTime Date), bool> hasInterestRateFor;
        private readonly CreateSavingsAccountBusinessEventManager createAccountMgr;
        private readonly SavingsContextFactory contextFactory;
        private readonly ICustomerRelationsMergeService relationsMergeService;

        public SavingsAccountCreationService(
            ICoreClock clock, ILoggingService loggingService, ICustomerClient customerClient,
            Action<(string SavingsAccountNr, ISavingsContext Context, string SendingLocation)> sendWelcomeEmail,
            Func<(SavingsAccountTypeCode AccountType, DateTime Date), bool> hasInterestRateFor,
            CreateSavingsAccountBusinessEventManager createAccountMgr, SavingsContextFactory contextFactory,
            ICustomerRelationsMergeService relationsMergeService)
        {
            this.clock = clock;
            this.loggingService = loggingService;
            this.customerClient = customerClient;
            this.sendWelcomeEmail = sendWelcomeEmail;
            this.hasInterestRateFor = hasInterestRateFor;
            this.createAccountMgr = createAccountMgr;
            this.contextFactory = contextFactory;
            this.relationsMergeService = relationsMergeService;
        }

        public CreateSavingsAccountResponse CreateAccount(CreateSavingsAccountRequest request)
        {
            var applicationItems = request?.ApplicationItems;
            var externalVariables = request?.ExternalVariables;
            var allowSavingsAccountNrGeneration = request?.AllowSavingsAccountNrGeneration;
            var allowCreateWithoutSignedAgreement = request?.AllowCreateWithoutSignedAgreement;

            if (applicationItems == null)
            {
                throw new NTechCoreWebserviceException("items missing") { IsUserFacing = true, ErrorHttpStatusCode = 400 };
            }

            var today = clock.Today;
            using (var context = contextFactory.CreateContext())
            {
                string failedMessage;

                var savingsAccountTypeCode = applicationItems?.Where(x => x.Name == SavingsApplicationItemName.savingsAccountTypeCode.ToString()).FirstOrDefault()?.Value;
                var savingsAccountTypeCodeP = Enums.Parse<SavingsAccountTypeCode>(savingsAccountTypeCode);
                if (!savingsAccountTypeCodeP.HasValue)
                    throw new NTechCoreWebserviceException("Missing or invalid savingsAccountTypeCode") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

                if (!hasInterestRateFor((savingsAccountTypeCodeP.Value, clock.Today)))
                {
                    throw new NTechCoreWebserviceException("Account type has no active interest rate") { IsUserFacing = true, ErrorHttpStatusCode = 400 };
                }

                var creationOptions = new CreateSavingsAccountBusinessEventManager.CreationOptions
                {
                    AllowAccountNrGeneration = allowSavingsAccountNrGeneration.GetValueOrDefault(),
                    AllowCreateWithoutSignedAgreement = allowCreateWithoutSignedAgreement.GetValueOrDefault()
                };

                SavingsAccountHeader savingsAccount;
                IOcrNumber ocrPaymentReference;
                var isOk = createAccountMgr.TryCreateSavingsAccount(
                    context,
                    applicationItems?.Select(x => Tuple.Create(x.Name, x.Value))?.ToList(),
                    externalVariables?.Select(x => Tuple.Create(x.Name, x.Value))?.ToList(),
                    out failedMessage,
                    out savingsAccount,
                    out ocrPaymentReference,
                    creationOptions: creationOptions);

                context.SaveChanges();

                if (!isOk)
                    throw new NTechCoreWebserviceException(failedMessage) { IsUserFacing = true, ErrorHttpStatusCode = 400 };
                else
                {
                    try
                    {
                        customerClient.MergeCustomerRelations(new List<CustomerClientCustomerRelation>
                        {
                            new CustomerClientCustomerRelation
                            {
                                CustomerId = savingsAccount.MainCustomerId,
                                RelationId = savingsAccount.SavingsAccountNr,
                                RelationType = $"SavingsAccount_{(savingsAccount.AccountTypeCode ?? SavingsAccountTypeCode.StandardAccount.ToString())}",
                                StartDate = clock.Today,
                                EndDate = null
                            }
                        });
                        relationsMergeService.MergeSavingsAccountsToCustomerRelations(onlySavingsAccountNrs: new HashSet<string> { savingsAccount.SavingsAccountNr });
                    }
                    catch (Exception ex)
                    {
                        loggingService.Error(ex, $"Failed to merge savings account relation on new account {savingsAccount?.SavingsAccountNr}");
                    }

                    sendWelcomeEmail((savingsAccount.SavingsAccountNr, context, $"OnCreate_{savingsAccount.SavingsAccountNr}"));

                    context.SaveChanges();

                    return new CreateSavingsAccountResponse
                    {
                        savingsAccountNr = savingsAccount.SavingsAccountNr,
                        status = savingsAccount.Status,
                        ocrPaymentReference = ocrPaymentReference.NormalForm
                    };
                }
            }
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