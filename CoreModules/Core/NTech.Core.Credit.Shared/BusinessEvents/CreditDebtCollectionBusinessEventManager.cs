using nCredit.Code.Fileformats;
using nCredit.Code.Services;
using nCredit.DomainModel;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace nCredit.DbModel.BusinessEvents
{
    public class CreditDebtCollectionBusinessEventManager : BusinessEventManagerOrServiceBase
    {
        private readonly IDocumentClient documentClient;
        private readonly ICreditEnvSettings envSettings;
        private readonly CreditTermsChangeCancelOnlyBusinessEventManager termsChangeBusinessEventManager;
        private readonly ICustomerClient customerClient;
        private readonly DebtCollectionCandidateService debtCollectionCandidateService;
        private readonly PaymentOrderService paymentOrderService;

        public CreditDebtCollectionBusinessEventManager(INTechCurrentUserMetadata currentUser, IDocumentClient documentClient,
            ICoreClock clock, IClientConfigurationCore clientConfiguration,
            ICreditEnvSettings envSettings, CreditTermsChangeCancelOnlyBusinessEventManager termsChangeBusinessEventManager, ICustomerClient customerClient,
            DebtCollectionCandidateService debtCollectionCandidateService, PaymentOrderService paymentOrderService) : base(currentUser, clock, clientConfiguration)
        {
            this.documentClient = documentClient;
            this.envSettings = envSettings;
            this.termsChangeBusinessEventManager = termsChangeBusinessEventManager;
            this.customerClient = customerClient;
            this.debtCollectionCandidateService = debtCollectionCandidateService;
            this.paymentOrderService = paymentOrderService;
        }

        public int SendEligibleCreditsToDebtCollection(ICreditContextExtended c, out IDictionary<string, string> skippedCreditNrsWithReasons)
        {
            var creditNrs = new HashSet<string>(debtCollectionCandidateService.GetEligibleForDebtCollectionCreditNrs(c));
            return SendCreditsToDebtCollection(creditNrs, c, out skippedCreditNrsWithReasons);
        }

        private void ThrowIfBothCompanyLoanAndUnsecuredLoanEnabled()
        {
            if (envSettings.IsCompanyLoansEnabled && envSettings.IsUnsecuredLoansEnabled)
                throw new Exception("Both company loans and unsecured loans cannot be enabled at the same time");
        }

        public int SendCreditsToDebtCollection(ISet<string> creditNrs, ICreditContextExtended c, out IDictionary<string, string> skippedCreditNrsWithReasons,
            Action<Dictionary<string, DebtCollectionNotNotifiedInterest>> observeNotNotifiedInterestPerCreditNr = null)
        {
            ThrowIfBothCompanyLoanAndUnsecuredLoanEnabled();

            var isCompanyLoansEnabled = envSettings.IsCompanyLoansEnabled;

            c.IsChangeTrackingEnabled = false;
            try
            {
                skippedCreditNrsWithReasons = new Dictionary<string, string>();

                if (creditNrs.Count == 0)
                    return 0;

                var fileModel = new DebtCollectionFileModel
                {
                    ExternalId = $"DF-{Guid.NewGuid().ToString()}",
                    Credits = new List<DebtCollectionFileModel.Credit>()
                };

                var evt = new BusinessEvent
                {
                    BookKeepingDate = Clock.Today,
                    ChangedById = UserId,
                    ChangedDate = Clock.Now,
                    EventDate = Clock.Today,
                    EventType = BusinessEventType.CreditDebtCollectionExport.ToString(),
                    TransactionDate = Clock.Today,
                    InformationMetaData = InformationMetadata
                };

                var customerIdsPerCreditNr = new Dictionary<string, HashSet<int>>();
                var customerRolesPerCreditNr = new Dictionary<string, Dictionary<int, HashSet<string>>>();
                Action<string, int, string> addCustomerIdForCreditWithRole = (creditNr, customerId, role) =>
                {
                    if (!customerIdsPerCreditNr.ContainsKey(creditNr))
                        customerIdsPerCreditNr[creditNr] = new HashSet<int>();
                    customerIdsPerCreditNr[creditNr].Add(customerId);
                    if (!customerRolesPerCreditNr.ContainsKey(creditNr))
                        customerRolesPerCreditNr[creditNr] = new Dictionary<int, HashSet<string>>();

                    var r = customerRolesPerCreditNr[creditNr];
                    if (!r.ContainsKey(customerId))
                        r[customerId] = new HashSet<string>();
                    r[customerId].Add(role);
                };

                var paymentOrder = paymentOrderService.GetPaymentOrderItems();
                var notificationModels = CreditNotificationDomainModel.CreateForSeveralCredits(creditNrs, c, paymentOrder, onlyFetchOpen: false);

                var notificationIds = notificationModels.SelectMany(x => x.Value.Values).Select(x => x.NotificationId).ToList();
                var notificationHeadersRaw = c.CreditNotificationHeadersQueryable.Where(x => notificationIds.Contains(x.Id)).ToDictionary(x => x.Id);
                var creditModels = CreditDomainModel.PreFetchForCredits(c, creditNrs.ToArray(), envSettings);

                var creditInfoByCreditNr = c.CreditHeadersQueryable.Select(x => new { Credit = x, CreditCustomers = x.CreditCustomers }).Where(x => creditNrs.Contains(x.Credit.CreditNr)).ToDictionary(x => x.Credit.CreditNr);

                var customers = new Dictionary<int, DebtCollectionFileModel.Customer>();

                //Applicants
                var creditCustomers = creditInfoByCreditNr.SelectMany(x => x.Value.CreditCustomers.Select(y => new { x.Value.Credit.CreditNr, y.CustomerId, y.ApplicantNr })).ToList();
                creditCustomers.ForEach(x => addCustomerIdForCreditWithRole(x.CreditNr, x.CustomerId, isCompanyLoansEnabled ? "company" : "applicant"));
                FetchCustomers(creditCustomers.Select(x => x.CustomerId).ToHashSetShared(), customers);

                //Collaterals
                if (isCompanyLoansEnabled)
                {
                    var collateralCustomers = c
                        .CreditHeadersQueryable
                        .Where(x => creditNrs.Contains(x.CreditNr))
                        .SelectMany(x => x.CustomerListMembers.Where(y => y.ListName == "companyLoanCollateral").Select(y => new { x.CreditNr, y.CustomerId }))
                        .ToList();

                    foreach (var col in collateralCustomers)
                    {
                        addCustomerIdForCreditWithRole(col.CreditNr, col.CustomerId, "collateral");
                    }
                    FetchCustomers(collateralCustomers.Select(x => x.CustomerId).ToHashSetShared(), customers);
                }

                var validTerminationLetters = c
                    .CreditTerminationLetterHeadersQueryable
                    .Where(x => creditNrs.Contains(x.CreditNr) && !x.Credit.Notifications.Any(y => x.CreditNr == y.CreditNr && y.DueDate > x.DueDate))
                    .ToList()
                    .GroupBy(x => x.CreditNr)
                    .ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.DueDate).First());

                var count = 0;
                var actuallyExportedCreditNrs = new HashSet<string>();
                var notNotifiedInterestPerCreditNr = new Dictionary<string, DebtCollectionNotNotifiedInterest>();

                foreach (var creditNr in creditNrs)
                {
                    var creditModel = creditModels[creditNr];
                    
                    var notifications = notificationModels[creditNr];
                    var creditHeader = creditInfoByCreditNr[creditNr].Credit;
                    
                    var status = creditModel.GetStatus();
                    if (status != CreditStatus.Normal)
                    {
                        skippedCreditNrsWithReasons[creditNr] = $"Status {status.ToString()} != Normal";
                        continue;
                    }

                    if (!validTerminationLetters.ContainsKey(creditNr))
                    {
                        skippedCreditNrsWithReasons[creditNr] = $"There is no valid termination letter";
                        continue;
                    }

                    var pausedUntilDate = creditModel.GetDebtCollectionPausedUntilDate(Clock.Today);
                    if (pausedUntilDate.HasValue && pausedUntilDate.Value > Clock.Today)
                    {
                        skippedCreditNrsWithReasons[creditNr] = $"Paused for debt collection until {pausedUntilDate.Value.ToString("yyyy-MM-dd")}";
                        continue;
                    }

                    var fileCreditModel = new DebtCollectionFileModel.Credit
                    {
                        CreditNr = creditModel.CreditNr,
                        ApplicantNrByCustomerId = creditCustomers.Where(x => x.CreditNr == creditModel.CreditNr).ToDictionary(x => x.CustomerId, x => x.ApplicantNr),
                        IsCompanyLoan = creditModel.GetCreditType() == CreditType.CompanyLoan,
                        Currency = ClientCfg.Country.BaseCurrency,
                        Ocr = creditModel.GetOcrPaymentReference(Clock.Today),
                        StartDate = creditModel.GetStartDate().Date.Date,
                        InterestRatePercent = creditModel.GetInterestRatePercent(Clock.Today),
                        TerminationLetterDueDate = validTerminationLetters[creditNr].DueDate,
                        CapitalizedInitialFeeAmount = creditModel.GetTotalBusinessEventAmount(BusinessEventType.CapitalizedInitialFee, TransactionAccountType.CapitalDebt, Clock.Today),
                        NewCreditCapitalAmount = creditModel.GetTotalBusinessEventAmount(BusinessEventType.NewCredit, TransactionAccountType.CapitalDebt, Clock.Today),
                        AdditionalLoanCapitalAmount = creditModel.GetTotalBusinessEventAmount(BusinessEventType.NewAdditionalLoan, TransactionAccountType.CapitalDebt, Clock.Today),
                        InitialLoanCampaignCode = creditModel.GetIntialLoanCampaignCode(Clock.Today)
                    };
                    fileModel.Credits.Add(fileCreditModel);

                    AddDatedCreditString(DatedCreditStringCode.DebtCollectionFileExternalId.ToString(), fileModel.ExternalId, creditHeader, evt, c);

                    fileCreditModel.OrderedCustomersWithRoles = customerIdsPerCreditNr[creditModel.CreditNr]
                        .OrderBy(x => fileCreditModel.ApplicantNrByCustomerId.OptS(x) ?? 999) //999 = non applicant roles placed last
                        .ThenBy(x => x)
                        .Select(x => Tuple.Create(customers[x], customerRolesPerCreditNr[creditModel.CreditNr][x]))
                        .ToList();

                    var totalDebtCollectionAmounts = new Dictionary<string, decimal>();
                    paymentOrder.Select(x => x.GetUniqueId()).ToList().ForEach(x => totalDebtCollectionAmounts[x] = 0m);

                    Lazy<WriteoffHeader> wo = new Lazy<WriteoffHeader>(() =>
                    {
                        var w = new WriteoffHeader
                        {
                            BookKeepingDate = evt.BookKeepingDate,
                            ChangedById = evt.ChangedById,
                            ChangedDate = evt.ChangedDate,
                            InformationMetaData = evt.InformationMetaData,
                            TransactionDate = evt.TransactionDate
                        };
                        c.AddWriteoffHeaders(w);
                        return w;
                    });

                    //Write off capital debt
                    var capitalBalance = creditModel.GetBalance(CreditDomainModel.AmountType.Capital, Clock.Today);
                    if (capitalBalance > 0m)
                    {
                        totalDebtCollectionAmounts[PaymentOrderItem.FromAmountType(CreditDomainModel.AmountType.Capital).GetUniqueId()] += capitalBalance;
                        c.AddAccountTransactions(CreateTransaction(
                            TransactionAccountType.CapitalDebt,
                            -capitalBalance,
                            evt.BookKeepingDate,
                            evt,
                            creditNr: creditNr,
                            writeOff: wo.Value));
                    }

                    //Write off any open notifications
                    fileCreditModel.Notifications = new List<DebtCollectionFileModel.Notification>();
                    var movedBackNotNotifiedCapitalAmount = 0m;
                    foreach (var n in notifications.Where(x => !x.Value.GetClosedDate(Clock.Today).HasValue).OrderBy(x => x.Value.DueDate))
                    {
                        var notification = n.Value;
                        var fileNotificationModel = new DebtCollectionFileModel.Notification
                        {
                            Amounts = new Dictionary<string, decimal>(),
                            DueDate = notification.DueDate,
                            NotificationDate = notification.NotificationDate
                        };

                        var notificationRaw = notificationHeadersRaw[notification.NotificationId];
                        if (!notificationRaw.ClosedTransactionDate.HasValue)
                            notificationRaw.ClosedTransactionDate = Clock.Today;

                        fileCreditModel.Notifications.Add(fileNotificationModel);

                        foreach (var paymentOrderItem in paymentOrder)
                        {
                            var uniqueId = paymentOrderItem.GetUniqueId();
                            if(paymentOrderItem.IsCreditDomainModelAmountType(CreditDomainModel.AmountType.Capital))
                            {
                                fileNotificationModel.Amounts[uniqueId] = notification.GetRemainingBalance(Clock.Today, paymentOrderItem);
                                if (fileNotificationModel.Amounts[uniqueId] > 0m)
                                {
                                    //Move it back to not notified capital
                                    movedBackNotNotifiedCapitalAmount += fileNotificationModel.Amounts[uniqueId];

                                    c.AddAccountTransactions(CreateTransaction(
                                        TransactionAccountType.NotNotifiedCapital,
                                        fileNotificationModel.Amounts[uniqueId],
                                        evt.BookKeepingDate,
                                        evt,
                                        creditNr: creditNr,
                                        notificationId: notification.NotificationId,
                                        writeOff: wo.Value));
                                }
                            }
                            else
                            {
                                fileNotificationModel.Amounts[uniqueId] = notification.GetRemainingBalance(Clock.Today, paymentOrderItem);
                                if (fileNotificationModel.Amounts[uniqueId] > 0m)
                                {
                                    totalDebtCollectionAmounts[uniqueId] += fileNotificationModel.Amounts[uniqueId];
                                    c.AddAccountTransactions(CreateTransaction(
                                        paymentOrderItem.IsBuiltin 
                                            ? CreditNotificationDomainModel.MapNonCapitalAmountTypeToAccountType(paymentOrderItem.GetBuiltinAmountType())
                                            : TransactionAccountType.NotificationCost,
                                        -fileNotificationModel.Amounts[uniqueId],
                                        evt.BookKeepingDate,
                                        evt,
                                        creditNr: creditNr,
                                        notificationId: notification.NotificationId,
                                        writeOff: wo.Value,
                                        subAccountCode: paymentOrderItem.IsBuiltin ? null : paymentOrderItem.Code));
                                }
                            }
                        }
                    }

                    //Write off not notified capital
                    fileCreditModel.NotNotifiedCapitalAmount = creditModel.GetNotNotifiedCapitalBalance(Clock.Today);
                    var notNotifiedCapitalBalance = fileCreditModel.NotNotifiedCapitalAmount + movedBackNotNotifiedCapitalAmount;
                    if (notNotifiedCapitalBalance > 0m)
                    {
                        c.AddAccountTransactions(CreateTransaction(
                            TransactionAccountType.NotNotifiedCapital,
                            -notNotifiedCapitalBalance,
                            evt.BookKeepingDate,
                            evt,
                            creditNr: creditNr,
                            writeOff: wo.Value));
                    }

                    fileCreditModel.NextInterestDate = creditModel.GetNextInterestFromDate(Clock.Today);
                    var b = new StringBuilder();
                    var amountSpec = string.Join("; ", totalDebtCollectionAmounts.Select(x => $"{x.Key}= {x.Value.ToString("C", CommentFormattingCulture)}"));
                    var comment = $"Exported to debt collection. Total amount due: {totalDebtCollectionAmounts.Sum(x => x.Value).ToString("C", CommentFormattingCulture)}. Next interest date: {fileCreditModel.NextInterestDate.ToString("d", CommentFormattingCulture)}. Amount parts: {amountSpec}";

                    AddComment(comment, BusinessEventType.CreditDebtCollectionExport, c, creditNr: creditNr);

                    SetStatus(creditHeader, CreditStatus.SentToDebtCollection, evt, c);

                    notNotifiedInterestPerCreditNr[creditNr] = new DebtCollectionNotNotifiedInterest
                    {
                        Amount = creditModel.ComputeNotNotifiedInterestUntil(Clock.Today, Clock.Today, out var nrOfInterestDays),
                        FromDate = fileCreditModel.NextInterestDate,
                        NrOfInterestDays = nrOfInterestDays
                    };

                    actuallyExportedCreditNrs.Add(creditNr);
                    count += 1;
                }

                if (count == 0)
                    return 0;

                foreach (var termChangeId in termsChangeBusinessEventManager.GetActiveTermChangeIdsOnCredits(c, actuallyExportedCreditNrs))
                {
                    string fm;
                    if (!termsChangeBusinessEventManager.TryCancelCreditTermsChange(c, termChangeId, false, out fm, additionalReasonMessage: " by debt collection export"))
                    {
                        throw new Exception($"Failed to cancel pending credit term changes on {termChangeId}");
                    }
                }

                c.AddBusinessEvent(evt);

                //Create an export file

                var builder = new DebtCollectionFileFormat_Excel();
                observeNotNotifiedInterestPerCreditNr?.Invoke(notNotifiedInterestPerCreditNr);
                var excelKey = builder.CreateExcelFileInArchive(fileModel, Clock.Now, $"DebtCollectionExport_{Clock.Now.ToString("yyyy-MM-dd_HHmm")}.xlsx",
                    documentClient, notNotifiedInterestPerCreditNr, paymentOrderService);

                var fileArchiveKey = CreateExportFile(creditNrs, c, fileModel, creditModels);

                c.AddOutgoingDebtCollectionFileHeaders(new OutgoingDebtCollectionFileHeader
                {
                    ExternalId = fileModel.ExternalId,
                    FileArchiveKey = fileArchiveKey,
                    XlsFileArchiveKey = excelKey,
                    ChangedById = UserId,
                    ChangedDate = Clock.Now,
                    InformationMetaData = InformationMetadata,
                    TransactionDate = Clock.Today
                });

                c.DetectChanges();

                return count;
            }
            finally
            {
                c.IsChangeTrackingEnabled = true;
            }
        }

        private string CreateExportFile(ISet<string> creditNrs, ICreditContextExtended c, DebtCollectionFileModel fileModel, IDictionary<string, CreditDomainModel> creditModels)
        {
            if (envSettings.DebtCollectionPartnerName?.Equals("LindorffFi", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                var settings = new DebtCollectionFileFormat_Lindorff.LindorfSettings
                {
                    ClientNumber = envSettings.LindorffFileDebtCollectionClientNumber
                };
                var format = new DebtCollectionFileFormat_Lindorff();
                return format.CreateFileInArchive(fileModel, settings, Clock.Now, $"DebtCollectionExport_{Clock.Now.ToString("yyyy-MM-dd_HHmm")}.txt", documentClient);
            }
            else if (envSettings.DebtCollectionPartnerName?.Equals("OkpFi", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                var notNotifiedInterestUntilTerminationLetterDueDateByCreditNr = fileModel.Credits.ToDictionary(x => x.CreditNr, x =>
                {
                    int _;
                    var cm = creditModels[x.CreditNr];
                    return cm.ComputeNotNotifiedInterestUntil(Clock.Today, x.TerminationLetterDueDate, out _);
                });

                var initialEffectiveInterestRateByCreditNr = DebtCollectionFileFormat_OkpFi.GetInitialEffectiveInterstRateForCredits(creditNrs, c, envSettings);
                var format = new DebtCollectionFileFormat_OkpFi();
                return format.CreateExcelFileInArchive(fileModel, Clock.Now, $"DebtCollectionExport_Okp_{Clock.Now.ToString("yyyy-MM-dd_HHmm")}.xlsx", initialEffectiveInterestRateByCreditNr, notNotifiedInterestUntilTerminationLetterDueDateByCreditNr, 
                    documentClient, paymentOrderService);
            }
            else
                return null;
        }

        private void FetchCustomers(HashSet<int> customerIds, Dictionary<int, DebtCollectionFileModel.Customer> customers)
        {
            var existingCustomerIds = customerIds.Intersect(customers.Keys);
            var newCustomerIds = customerIds.Except(existingCustomerIds).ToList();

            if (newCustomerIds.Count == 0)
                return;

            Func<string, string> langFromCountry = country =>
            {
                if (country == "FI")
                    return "fi";
                else if (country == "SE")
                    return "sv";
                else
                    return "en";
            };
            var result = customerClient.BulkFetchPropertiesByCustomerIdsD(customerIds,
                "civicRegNr", "firstName", "lastName", "addressStreet", "addressZipcode", "addressCity", "addressCountry", "phone", "email", "isCompany", "companyName", "orgnr");

            foreach (var r in result)
            {
                var p = r.Value.ToDictionary(x => x.Key, x => x.Value, StringComparer.InvariantCultureIgnoreCase);
                var isCompany = p.GetN("isCompany") == "true";
                customers[r.Key] = new DebtCollectionFileModel.Customer
                {
                    CivicRegNrOrOrgnr = isCompany ? p.GetN("orgnr") : p.GetN("civicRegNr"),
                    CustomerId = r.Key,
                    IsCompany = isCompany,
                    Email = p.GetN("email"),
                    Phone = p.GetN("phone"),
                    FirstName = isCompany ? null : p.GetN("firstName"),
                    LastName = isCompany ? null : p.GetN("lastName"),
                    CompanyName = isCompany ? p.GetN("companyName") : null,
                    PreferredLanguage = langFromCountry(ClientCfg.Country.BaseCountry),
                    CivicRegNrOrOrgnrCountry = ClientCfg.Country.BaseCountry,
                    Adr = new DebtCollectionFileModel.Address
                    {
                        City = p.GetN("addressCity"),
                        Street = p.GetN("addressStreet"),
                        Zipcode = p.GetN("addressZipcode"),
                        Country = p.GetN("addressCountry") ?? ClientCfg.Country.BaseCountry
                    }
                };
            }
        }

        public bool TryPostponeOrResumeDebtCollection(string creditNr, ICreditContextExtended context, DateTime? postponeUntilDate, out string failureMessage)
        {
            if (!context.CreditHeadersQueryable.Any(x => x.CreditNr == creditNr))
            {
                failureMessage = "Credit does not exist";
                return false;
            }

            var date = postponeUntilDate.HasValue ? postponeUntilDate.Value : Clock.Today;
            var isPostpone = postponeUntilDate.HasValue;

            var evt = new BusinessEvent
            {
                EventDate = Now,
                EventType = isPostpone ? BusinessEventType.PostponeDebtCollectionExport.ToString() : BusinessEventType.ResumeDebtCollectionExport.ToString(),
                BookKeepingDate = Now.ToLocalTime().Date,
                TransactionDate = Now.ToLocalTime().Date,
                ChangedById = UserId,
                ChangedDate = Now,
                InformationMetaData = InformationMetadata
            };
            context.AddBusinessEvent(evt);

            context.AddDatedCreditDate(new DatedCreditDate
            {
                CreditNr = creditNr,
                BusinessEvent = evt,
                TransactionDate = Now.ToLocalTime().Date,
                Name = DatedCreditDateCode.DebtCollectionPausedUntilDate.ToString(),
                Value = date,
                ChangedById = UserId,
                ChangedDate = Now,
                InformationMetaData = InformationMetadata
            });

            AddComment(
                isPostpone ? $"Debt collection export paused until {date.ToString("yyyy-MM-dd")}" : "Debt collection export manually resumed",
                isPostpone ? BusinessEventType.PostponeDebtCollectionExport : BusinessEventType.ResumeDebtCollectionExport,
                context,
                creditNr: creditNr);

            failureMessage = null;
            return true;
        }
    }

    public static class DictExt
    {
        public static TValue GetN<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key) where TKey : class, TValue
        {
            if (source?.ContainsKey(key) ?? false)
                return source[key];
            else
                return default(TValue);
        }
    }

    public class DebtCollectionNotNotifiedInterest
    {
        public decimal Amount { get; set; }
        public DateTime FromDate { get; set; }
        public int NrOfInterestDays { get; set; }
    }
}