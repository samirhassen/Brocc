using nCredit.Code;
using nCredit.Code.Services;
using nCredit.DbModel.DomainModel;
using nCredit.DomainModel;
using Newtonsoft.Json;
using NTech;
using NTech.Core;
using NTech.Core.Credit.Shared.BusinessEvents.Utilities;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace nCredit.DbModel.BusinessEvents
{
    public class NewCreditTerminationLettersBusinessEventManager : BusinessEventManagerOrServiceBase
    {
        private readonly PaymentAccountService paymentAccountService;
        private readonly INotificationProcessSettingsFactory notificationProcessSettingsFactory;
        private readonly CreditContextFactory creditContextFactory;
        private readonly ICreditEnvSettings envSettings;
        private readonly TerminationLetterCandidateService terminationLetterCandidateService;
        private readonly ILoggingService loggingService;
        private readonly PaymentOrderService paymentOrderService;
        private readonly OcrNumberParser ocrNrParser;

        public NewCreditTerminationLettersBusinessEventManager(INTechCurrentUserMetadata currentUser, PaymentAccountService paymentAccountService, ICoreClock clock, IClientConfigurationCore clientConfiguration,
            INotificationProcessSettingsFactory notificationProcessSettingsFactory, CreditContextFactory creditContextFactory,
            ICreditEnvSettings envSettings, TerminationLetterCandidateService terminationLetterCandidateService, ILoggingService loggingService,
            PaymentOrderService paymentOrderService) : base(currentUser, clock, clientConfiguration)
        {
            this.paymentAccountService = paymentAccountService;
            this.notificationProcessSettingsFactory = notificationProcessSettingsFactory;
            this.creditContextFactory = creditContextFactory;
            this.envSettings = envSettings;
            this.terminationLetterCandidateService = terminationLetterCandidateService;
            this.loggingService = loggingService;
            this.paymentOrderService = paymentOrderService;
            ocrNrParser = new OcrNumberParser(clientConfiguration.Country.BaseCountry);
        }

        public string[] GetEligibleForTerminationLetterCreditNrs()
        {
            using (var context = creditContextFactory.CreateContext())
            {
                return terminationLetterCandidateService.GetEligibleForTerminationLetterCreditNrs(context).OrderBy(x => x).ToArray();
            }
        }

        public HashSet<string> CreateTerminationLettersForEligibleCredits(Func<IDictionary<string, object>, string, bool, string> renderToArchive, ICustomerPostalInfoRepository customerPostalInfoRepository, CreditType forCreditType)
        {
            var creditNrs = GetEligibleForTerminationLetterCreditNrs();
            return CreateTerminationLettersForSpecificCreditNrs(renderToArchive, creditNrs, customerPostalInfoRepository, forCreditType);
        }

        public HashSet<string> CreateTerminationLettersForSpecificCreditNrs(Func<IDictionary<string, object>, string, bool, string> renderToArchive, string[] creditNrs, ICustomerPostalInfoRepository customerPostalInfoRepository, CreditType forCreditType)
        {
            var creditNrsWithLettersCreated = new HashSet<string>();
            using (var context = creditContextFactory.CreateContext())
            {
                var coTerminationResult = GroupForCoTermination(creditNrs, context);

                foreach (var terminationGroup in SplitIntoGroupsOfN(coTerminationResult.SingleCredits, 20))
                {
                    creditNrsWithLettersCreated.AddRange(TerminateGroup(renderToArchive, terminationGroup, customerPostalInfoRepository, forCreditType, context, isCoTerminatedGroup: false));
                    context.SaveChanges(); //Intentionally save after each batch. The system recovers from this if some fail and others succeed
                }

                foreach (var coTerminationGroup in SplitIntoGroupsOfN(coTerminationResult.CoTerminationGroups, 10))
                {
                    creditNrsWithLettersCreated.AddRange(TerminateGroup(renderToArchive, coTerminationGroup, customerPostalInfoRepository, forCreditType, context, isCoTerminatedGroup: true));
                    context.SaveChanges(); //Intentionally save after each batch. The system recovers from this if some fail and others succeed
                }
            }
            return creditNrsWithLettersCreated;
        }

        private HashSet<string> TerminateGroup(Func<IDictionary<string, object>, string, bool, string> renderToArchive, IEnumerable<List<CreditToBeTerminated>> terminationGroup, ICustomerPostalInfoRepository customerPostalInfoRepository, 
            CreditType forCreditType, ICreditContextExtended context, bool isCoTerminatedGroup)
        {
            var creditNrsWithLettersCreated = new HashSet<string>();
            
            //TODO: This is another potential spot to expand the list across the mortgage loan agreement nr
            var creditNrsGroup = terminationGroup.SelectMany(x => x.Select(y => y.CreditNr)).ToList();
            var notificationModels = CreditNotificationDomainModel.CreateForSeveralCredits(new HashSet<string>(creditNrsGroup), context, paymentOrderService.GetPaymentOrderItems(), onlyFetchOpen: false);
            var creditModels = CreditDomainModel.PreFetchForCredits(context, creditNrsGroup.ToArray(), envSettings);

            var customerCards = GetApplicantCustomerInfoByCreditNrs(context, creditNrsGroup.ToList(), customerPostalInfoRepository, forCreditType == CreditType.CompanyLoan, forCreditType == CreditType.MortgageLoan);
            var mlObjectName = forCreditType == CreditType.MortgageLoan ? MortgageLoanCollateralService.GetPropertyIdByCreditNr(context, creditNrsGroup.ToHashSetShared(), true) : null;

            if(!isCoTerminatedGroup)
            {
                foreach (var creditNr in creditNrsGroup)
                {
                    var r = CreateTerminationLetter(context,
                        renderToArchive,
                        creditNr,
                        creditModels,
                        notificationModels,
                        customerCards[creditNr],
                        forCreditType == CreditType.MortgageLoan ? mlObjectName.Opt(creditNr) : null,
                        null);
                    if (r != null)
                    {
                        creditNrsWithLettersCreated.Add(creditNr);
                    }
                }
            }
            else
            {
                foreach(var coTerminatedCredits in terminationGroup)
                {
                    var coTerminationContext = new CoTerminationContext(coTerminatedCredits);
                    foreach (var credit in coTerminationContext.GetHandlingOrder())
                    {
                        var creditNr = credit.CreditNr;
                        var r = CreateTerminationLetter(context,
                            renderToArchive,
                            creditNr,
                            creditModels,
                            notificationModels,
                            customerCards[creditNr],
                            forCreditType == CreditType.MortgageLoan ? mlObjectName.Opt(creditNr) : null,
                            coTerminationContext);
                        if (r != null)
                        {
                            creditNrsWithLettersCreated.Add(creditNr);
                        }
                    }
                }
            }

            return creditNrsWithLettersCreated;
        }         

        public static bool HasTerminationLettersThatSuspendTheCreditProcess(IClientConfigurationCore clientConfiguration) =>
            clientConfiguration.Country.BaseCountry == "SE";

        private object CreateTerminationLetter(
            ICreditContextExtended context,
            Func<IDictionary<string, object>, string, bool, string> renderToArchive,
            string creditNr,
            IDictionary<string, CreditDomainModel> allCredits,
            Dictionary<string, Dictionary<int, CreditNotificationDomainModel>> allCreditsNotifications,
            List<TerminationLetterReceiverCustomerModel> customerCards,
            string mlObjectName,
            CoTerminationContext coTerminationContext)
        {
            var credit = allCredits[creditNr];
            var notifications = allCreditsNotifications[creditNr];

            var processSettings = notificationProcessSettingsFactory.GetByCreditType(credit.GetCreditType());
            var isCoTerminationMaster = coTerminationContext != null && coTerminationContext.IsCoTerminationMaster(credit.CreditNr);
            
            var evt = new BusinessEvent
            {
                EventDate = Now,
                EventType = BusinessEventType.NewTerminationLetter.ToString(),
                BookKeepingDate = Now.ToLocalTime().Date,
                TransactionDate = Now.ToLocalTime().Date,
                ChangedById = UserId,
                ChangedDate = Now,
                InformationMetaData = InformationMetadata
            };
            context.AddBusinessEvent(evt);

            var dueDate = GetTerminationLetterDueDate(processSettings, credit.CreditNr);

            var termination = new CreditTerminationLetterHeader
            {
                ChangedById = UserId,
                BookKeepingDate = Now.ToLocalTime().Date,
                PrintDate = Clock.Today,
                DueDate = dueDate,
                ChangedDate = Now,
                InformationMetaData = InformationMetadata,
                TransactionDate = Now.ToLocalTime().Date,
                CreditNr = credit.CreditNr,
                SuspendsCreditProcess = HasTerminationLettersThatSuspendTheCreditProcess(ClientCfg),
                CoTerminationId = coTerminationContext?.CoTerminationId,
                IsCoTerminationMaster = coTerminationContext == null ? new bool?() : isCoTerminationMaster
            };
            context.AddCreditTerminationLetterHeaders(termination);

            var printContextsPerCustomer = CreatePrintContextsPerCustomer(termination, notifications, credit, customerCards, mlObjectName, coTerminationContext);

            //Create the pdfs
            var archiveKeys = new List<string>();
            foreach (var customerAndContext in printContextsPerCustomer.OrderBy(x => x.Customer.ApplicantNr ?? x.Customer.CustomerId).ToList())
            {
                var applicantNr = customerAndContext.Customer.ApplicantNr;
                var customerCard = customerAndContext.Customer;
                var printContext = customerAndContext.PrintContext;

                if(coTerminationContext == null)
                {
                    var receiverSuffix = applicantNr.HasValue ? applicantNr.Value.ToString() : $"c{customerCard.CustomerId}";
                    var archiveKey = renderToArchive(
                            JsonConvert.DeserializeObject<ExpandoObject>(JsonConvert.SerializeObject(printContext)),
                            $"creditterminationletter_{termination.CreditNr}_{termination.DueDate:yyyy-MM-dd}_{receiverSuffix}.pdf",
                            false);

                    AddCreditDocument("TerminationLetter", applicantNr, archiveKey, context, terminationLetter: termination, customerId: customerCard.CustomerId);
                    archiveKeys.Add(archiveKey);
                }

                if (isCoTerminationMaster)
                {
                    //Co notification master
                    var printContexts = coTerminationContext.PrintContexts(customerCard.CustomerId);
                    var anySingleContext = printContexts.Last();                 

                    var masterPrintContext = new
                    {
                        anySingleContext.customerName,
                        anySingleContext.companyName,
                        anySingleContext.fullName,
                        anySingleContext.printDate,
                        anySingleContext.dueDate,
                        anySingleContext.streetAddress,
                        anySingleContext.areaAndZipcode,
                        anySingleContext.mlObjectName,
                        anySingleContext.paymentIban,
                        anySingleContext.paymentBankGiroNr,
                        mortgageLoanAgreementNr = credit.GetDatedCreditString(Clock.Today, DatedCreditStringCode.MortgageLoanAgreementNr, null, allowMissing: false),
                        sharedNotifiedOverdueDebt = coTerminationContext.NotifiedOverdueDebt(customerCard.CustomerId).ToString("C", PrintFormattingCulture),
                        sharedOcrPaymentReference = ocrNrParser.Parse(credit.GetDatedCreditString(Clock.Today, DatedCreditStringCode.SharedOcrPaymentReference, null, allowMissing: false)).DisplayForm,
                        sharedDueDate = coTerminationContext.MinDueDate(customerCard.CustomerId).ToString("d"),
                        sharedTotalDebt = coTerminationContext.TotalDebt(customerCard.CustomerId).ToString("C", PrintFormattingCulture),
                        sharedUnpaidNonOverdueAmountPerType = GetSharedUnpaidNonOverdueAmountPerType(coTerminationContext, allCredits, allCreditsNotifications),
                        terminations = printContexts
                    };
                    var receiverSuffix = applicantNr.HasValue ? applicantNr.Value.ToString() : $"c{customerCard.CustomerId}";
                    var archiveKey = renderToArchive(
                            JsonConvert.DeserializeObject<ExpandoObject>(JsonConvert.SerializeObject(masterPrintContext)),
                            $"creditterminationletter_{termination.CreditNr}_{termination.DueDate:yyyy-MM-dd}_{receiverSuffix}.pdf",
                            true);

                    AddCreditDocument("TerminationLetter", applicantNr, archiveKey, context, terminationLetter: termination, customerId: customerCard.CustomerId);
                    archiveKeys.Add(archiveKey);
                }
            }

            AddComment(
                $"Termination letter with due date {termination.DueDate.ToString("d", CommentFormattingCulture)} created",
                BusinessEventType.NewTerminationLetter,
                context,
                creditNr: credit.CreditNr,
                attachment: archiveKeys.Count == 0 ? null : CreditCommentAttachmentModel.ArchiveKeysOnly(archiveKeys));

            return termination;
        }

        private object GetSharedUnpaidNonOverdueAmountPerType(CoTerminationContext coTerminationContext, IDictionary<string, CreditDomainModel> allCredits, Dictionary<string, Dictionary<int, CreditNotificationDomainModel>> allCreditsNotifications)
        {
            var creditNrs = coTerminationContext.GetHandlingOrder().Select(x => x.CreditNr).ToList();
            var nonOverdueCapitalAmount = 0m;
            var nonOverdueInterestAmount = 0m;
            var nonOverdueNotificationFeeAmount = 0m;
            var nonOverdueReminderFeeAmount = 0m;
            var notNotifiedCapitalDebt = 0m;
            var nonOverdueCustomCosts = 0m;
            var customCostOrderItems = paymentOrderService.GetPaymentOrderUiItems().Where(x => !x.OrderItem.IsBuiltin).ToList();
            foreach (var creditNr in creditNrs)
            {
                if (allCreditsNotifications.ContainsKey(creditNr))
                {                    
                    foreach (var notification in allCreditsNotifications[creditNr].Values)
                    {
                        var isOverdue = notification.DueDate <= Clock.Today;
                        if(!isOverdue)
                        {
                            nonOverdueCapitalAmount += notification.GetRemainingBalance(Clock.Today, CreditDomainModel.AmountType.Capital);
                            nonOverdueInterestAmount += notification.GetRemainingBalance(Clock.Today, CreditDomainModel.AmountType.Interest);
                            nonOverdueNotificationFeeAmount += notification.GetRemainingBalance(Clock.Today, CreditDomainModel.AmountType.NotificationFee);
                            nonOverdueReminderFeeAmount += notification.GetRemainingBalance(Clock.Today, CreditDomainModel.AmountType.ReminderFee);
                            nonOverdueCustomCosts += customCostOrderItems.Sum(x => notification.GetRemainingBalance(Clock.Today, x.OrderItem));
                        }
                    }
                    notNotifiedCapitalDebt += allCredits[creditNr].GetNotNotifiedCapitalBalance(Clock.Today);
                }
            }
            var nonOverdueTotalAmount = nonOverdueCapitalAmount + nonOverdueInterestAmount + nonOverdueNotificationFeeAmount + nonOverdueReminderFeeAmount + notNotifiedCapitalDebt + nonOverdueCustomCosts;

            return new
            {
                nonOverdueCapitalAmount = nonOverdueCapitalAmount.ToString("C", PrintFormattingCulture),
                nonOverdueInterestAmount = nonOverdueInterestAmount.ToString("C", PrintFormattingCulture),
                nonOverdueNotificationFeeAmount = nonOverdueNotificationFeeAmount.ToString("C", PrintFormattingCulture),
                nonOverdueReminderFeeAmount = nonOverdueReminderFeeAmount.ToString("C", PrintFormattingCulture),
                notNotifiedCapitalDebt = notNotifiedCapitalDebt.ToString("C", PrintFormattingCulture),
                nonOverdueTotalAmount = nonOverdueTotalAmount.ToString("C", PrintFormattingCulture)
            };
        }

        private List<(TerminationLetterReceiverCustomerModel Customer, TerminationLetterPrintContext PrintContext)> CreatePrintContextsPerCustomer(CreditTerminationLetterHeader termination,
            IDictionary<int, CreditNotificationDomainModel> notifications, CreditDomainModel credit, List<TerminationLetterReceiverCustomerModel> customerCards, string mlObjectName, CoTerminationContext coTerminationContext)
        {
            var printContextPerCustomer = new List<(TerminationLetterReceiverCustomerModel Customer, TerminationLetterPrintContext PrintContext)>();

            var paymentOrder = paymentOrderService.GetPaymentOrderUiItems();
            
            var totalNotifiedCurrentAmount = 0m;
            var totalNotifiedOverdueCurrentAmount = 0m;
            var unpaidNonOverdueAmountPerType = new Dictionary<string, decimal>();
            var notificationsPrintData = notifications
                .Select(x => x.Value)
                .Where(x => !x.GetClosedDate(Clock.Today).HasValue)
                .OrderBy(x => x.DueDate)
                .Select(x =>
                {
                    var isOverdue = x.DueDate <= Clock.Today;
                    var currentAmount = x.GetRemainingBalance(Clock.Today);
                    totalNotifiedCurrentAmount += currentAmount;
                    if (isOverdue)
                    {
                        totalNotifiedOverdueCurrentAmount += currentAmount;
                    }
                    else
                    {
                        foreach(var orderItem in paymentOrder)
                        {
                            var currentItemAmount = x.GetRemainingBalance(Clock.Today, orderItem.OrderItem);
                            unpaidNonOverdueAmountPerType.AddOrUpdate(orderItem.UniqueId, currentItemAmount, prevBalance => prevBalance + currentItemAmount);
                        }
                    }

                    var a = new ExpandoObject() as IDictionary<string, object>;
                    a["currentAmount"] = currentAmount.ToString("C", PrintFormattingCulture);
                    a["dueDate"] = x.DueDate.ToString("d", PrintFormattingCulture);
                    a["notificationMonth"] = x.DueDate.ToString("yyyy.MM");
                    a["initialAmount"] = x.GetInitialAmount(Clock.Today).ToString("C", PrintFormattingCulture);
                    a["amountsList"] = NotificationAmountPrintContextModel.GetAmountsListPrintContext(x, paymentOrder, PrintFormattingCulture, Clock);
                    foreach (var am in paymentOrder.Where(y => y.OrderItem.IsBuiltin).Select(y => y.OrderItem.GetBuiltinAmountType()))
                    {
                        var balance = x.GetRemainingBalance(Clock.Today, am);
                        a[$"current{am}Amount"] = balance.ToString("C", PrintFormattingCulture);
                    }
                    return new { IsOverdue = isOverdue, PrintData = a };
                })
                .ToList();

            var printNotifications = notificationsPrintData.Select(x => x.PrintData).ToList();
            var overduePrintNotifications = notificationsPrintData.Where(x => x.IsOverdue).Select(x => x.PrintData).ToList();

            var notNotifiedCapitalDebt = credit.GetNotNotifiedCapitalBalance(Clock.Today);
            var totalDebt = notNotifiedCapitalDebt + totalNotifiedCurrentAmount;

            var printUnpaidNonOverdueAmountPerType = new Dictionary<string, object>();
            foreach (var orderItem in paymentOrder.Where(x => x.OrderItem.IsBuiltin).Select(x => x.OrderItem))
            {
                var amountType = orderItem.GetBuiltinAmountType();
                var amount = unpaidNonOverdueAmountPerType.OptS(orderItem.GetUniqueId());
                printUnpaidNonOverdueAmountPerType[$"nonOverdue{amountType}Amount"] = (amount ?? 0m).ToString("C", PrintFormattingCulture);
            }
            var nonOverdueCustomCostAmountsList = NotificationAmountPrintContextModel.GetAmountsListPrintContext(
                paymentOrder.Where(x => !x.OrderItem.IsBuiltin).ToList(), PrintFormattingCulture, 
                x => unpaidNonOverdueAmountPerType.OptS(x.UniqueId) ?? 0m);

            printUnpaidNonOverdueAmountPerType[$"nonOverdueTotalAmount"] =
                (unpaidNonOverdueAmountPerType.Values.Sum() + notNotifiedCapitalDebt).ToString("C", PrintFormattingCulture);

            foreach(var name in printUnpaidNonOverdueAmountPerType.Keys.Where(x => !x.EndsWith("NonZero")).ToList())
            {
                printUnpaidNonOverdueAmountPerType[$"{name}NonZero"] = ((string)printUnpaidNonOverdueAmountPerType[name]) != 0m.ToString("C", PrintFormattingCulture)
                    ? new bool?(true) : new bool();
            }

            var archiveKeys = new List<string>();            
            foreach (var customer in customerCards.OrderBy(x => x.ApplicantNr ?? x.CustomerId).ToList())
            {
                var applicantNr = customer.ApplicantNr;
                var customerCard = customer;
                var printContext = new TerminationLetterPrintContext
                {
                    customerName = customerCard.PostalInfo.GetCustomerName(),
                    companyName = customerCard.PostalInfo.GetCompanyPropertyOrNull(x => x.CompanyName),
                    fullName = customerCard.PostalInfo.GetPersonPropertyOrNull(x => x.FullName),
                    printDate = termination.PrintDate.ToString("d", PrintFormattingCulture),
                    dueDate = termination.DueDate.ToString("d", PrintFormattingCulture),
                    notifiedDebt = totalNotifiedCurrentAmount.ToString("C", PrintFormattingCulture),
                    notifiedOverdueDebt = totalNotifiedOverdueCurrentAmount.ToString("C", PrintFormattingCulture),
                    totalDebt = totalDebt.ToString("C", PrintFormattingCulture),
                    streetAddress = customerCard.PostalInfo.StreetAddress,
                    areaAndZipcode = $"{customerCard.PostalInfo.ZipCode} {customerCard.PostalInfo.PostArea}",
                    ocrPaymentReference = ocrNrParser.Parse(credit.GetOcrPaymentReference(Clock.Today)).DisplayForm,
                    paymentIban = ClientCfg.Country.BaseCountry == "FI" ? paymentAccountService.GetIncomingPaymentBankAccountNrRequireIbanFi().GroupsOfFourValue : null,
                    paymentBankGiroNr = ClientCfg.Country.BaseCountry == "SE" ? paymentAccountService.GetIncomingPaymentBankAccountNrRequireBankgiro().DisplayFormattedValue : null,
                    notifications = printNotifications,
                    overdueNotifications = overduePrintNotifications,
                    creditNr = credit.CreditNr,
                    unpaidNonOverdueAmountPerType = printUnpaidNonOverdueAmountPerType,
                    notNotifiedCapitalDebt = notNotifiedCapitalDebt.ToString("C", PrintFormattingCulture),
                    notNotifiedCapitalDebtNonZero = notNotifiedCapitalDebt != 0m ? new bool?(true) : new bool?(),
                    mlObjectName = mlObjectName,
                    amountTypes = NotificationAmountTextPrintContextModel.GetAmountTypes(paymentOrder),
                    nonOverdueCustomCostAmountsList = nonOverdueCustomCostAmountsList
                };
                printContextPerCustomer.Add((Customer: customer, PrintContext: printContext));
                coTerminationContext?.AddTermination(printContext, customer, totalNotifiedOverdueCurrentAmount, termination.DueDate, totalDebt);
            }

            return printContextPerCustomer;
        }

        private DateTime GetTerminationLetterDueDate(NotificationProcessSettings processSettings, string creditNr)
        {
            DateTime dueDate;
            if (ClientCfg.Country.BaseCountry == "SE")
            {
                const int LegalMinDueDays = 28;
                var today = Clock.Today;
                if (processSettings.TerminationLetterDueDay.HasValue)
                {
                    dueDate = new DateTime(today.Year, today.Month, processSettings.TerminationLetterDueDay.Value);
                    var dueDateLegalMin = today.AddDays(LegalMinDueDays);
                    if (dueDate < dueDateLegalMin)
                    {
                        loggingService.Warning($"Termination letter duedate for {creditNr} changed from {dueDate} to {dueDateLegalMin} to comply with the legally mandated minimum 28 due days");
                        dueDate = dueDateLegalMin;
                    }
                }
                else
                {
                    var dueDays = int.Parse(ClientCfg.OptionalSetting("ntech.credit.termination.duedays") ?? LegalMinDueDays.ToString());
                    if (dueDays < LegalMinDueDays)
                        throw new NTechCoreWebserviceException($"ntech.credit.termination.duedays must be >= {LegalMinDueDays} to comply with Swedish law");
                    dueDate = today.AddDays(dueDays);
                }
            }
            else
            {
                dueDate = processSettings.TerminationLetterDueDay.HasValue
                    ? new DateTime(Clock.Today.Year, Clock.Today.Month, processSettings.TerminationLetterDueDay.Value)
                    : Clock.Today.AddDays(int.Parse(ClientCfg.OptionalSetting("ntech.credit.termination.duedays") ?? "14"));
            }
            return dueDate;
        }

        public object CreateDeliveryExport(List<string> errors, IDocumentClient documentClient, ICustomerPostalInfoRepository customerPostalInfoRepository, CreditType forCreditType)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var terminationLetters = context
                    .CreditTerminationLetterHeadersQueryable
                    .Where(x => x.Credit.CreditType == forCreditType.ToString() && !x.OutgoingCreditTerminationLetterDeliveryFileHeaderId.HasValue)
                    .Select(x => new
                    {
                        TerminationLetter = x,
                        x.Documents,
                        Customers = x.Credit.CreditCustomers
                    })
                    .ToList();


                if (terminationLetters.Count == 0)
                {
                    return null;
                }

                customerPostalInfoRepository.PreFetchCustomerPostalInfo(new HashSet<int>(terminationLetters.SelectMany(x => x.Customers.Select(y => y.CustomerId))));

                var f = new OutgoingCreditTerminationLetterDeliveryFileHeader
                {
                    ChangedById = UserId,
                    ExternalId = Guid.NewGuid().ToString(),
                    InformationMetaData = InformationMetadata,
                    TransactionDate = Clock.Today,
                    ChangedDate = Clock.Now
                };

                var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                var tempZipfile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");
                Directory.CreateDirectory(tempFolder);
                try
                {
                    List<XElement> meta = new List<XElement>();
                    foreach (var n in terminationLetters)
                    {
                        var customerIdByApplicantNr = n.Customers.ToDictionary(x => x.ApplicantNr, x => x.CustomerId);

                        if (n.Documents == null || !n.Documents.Any())
                        {
                            errors.Add("Missing pdfs for credit " + n.TerminationLetter.CreditNr);
                        }
                        else
                        {
                            foreach (var d in n.Documents)
                            {
                                var customerId = d.CustomerId ?? customerIdByApplicantNr[d.ApplicantNr.Value];
                                var postalInfo = customerPostalInfoRepository.GetCustomerPostalInfo(customerId);

                                var (IsSuccess, ContentType, FileName, FileData) = documentClient.TryFetchRaw(d.ArchiveKey);
                                if (!IsSuccess)
                                {
                                    throw new NTechCoreWebserviceException($"Missing archive document {d.ArchiveKey} for credit {n.TerminationLetter.CreditNr} during termination letter delivery.") { ErrorCode = "missingTerminationLetterPdf" };
                                }

                                var pdfBytes = FileData;
                                var fileName = $"creditterminationletter_{n.TerminationLetter.CreditNr}_{n.TerminationLetter.DueDate:yyyy-MM-dd}_{(d.ApplicantNr.HasValue ? d.ApplicantNr.Value.ToString() : $"c{d.CustomerId.Value}")}.pdf";
                                System.IO.File.WriteAllBytes(Path.Combine(tempFolder, fileName), pdfBytes);
                                meta.Add(new XElement("CreditTerminationLetter",
                                    new XElement("CreditNr", n.TerminationLetter.CreditNr),
                                    new XElement("ApplicantNr", d.ApplicantNr),
                                    new XElement("CustomerId", d.CustomerId),
                                    new XElement("Name", postalInfo.GetCustomerName()),
                                    new XElement("Street", postalInfo.StreetAddress),
                                    new XElement("City", postalInfo.PostArea),
                                    new XElement("Zip", postalInfo.ZipCode),
                                    new XElement("Country", postalInfo.AddressCountry ?? ClientCfg.Country.BaseCountry),
                                    new XElement("PdfFileName", fileName)
                                    ));
                            }
                            n.TerminationLetter.DeliveryFile = f;
                        }
                    }

                    XDocument metaDoc = new XDocument(new XElement("CreditTerminationLetters",
                        new XAttribute("creationDate", Clock.Now.ToString("o")),
                        new XAttribute("deliveryId", f.ExternalId),
                        meta));

                    metaDoc.Save(Path.Combine(tempFolder, "creditterminationletter_metadata.xml"));

                    var fs = new ICSharpCode.SharpZipLib.Zip.FastZip();

                    fs.CreateZip(tempZipfile, tempFolder, true, null);

                    var filename = $"creditterminationletter_{Clock.Today:yyyy-MM}_{f.ExternalId}.zip";
                    f.FileArchiveKey = documentClient.ArchiveStoreFile(
                        new FileInfo(tempZipfile),
                        "application/zip",
                        filename);

                    context.SaveChanges();

                    if (envSettings.OutgoingCreditNotificationDeliveryFolder != null)
                    {
                        var targetFolder = envSettings.OutgoingCreditNotificationDeliveryFolder;
                        targetFolder.Create();
                        System.IO.File.Copy(tempZipfile, Path.Combine(targetFolder.FullName, filename));
                    }

                    return f;
                }
                finally
                {
                    try
                    {
                        Directory.Delete(tempFolder, true);
                        if (File.Exists(tempZipfile))
                        {
                            File.Delete(tempZipfile);
                        }
                    }
                    catch { /* ignored*/ }

                }
            }
        }

        private class TerminationLetterPrintContext
        {

            public string creditNr { get; set; }
            public string customerName { get; set; }
            public string fullName { get; set; }
            public string companyName { get; set; }
            public string streetAddress { get; set; }
            public string areaAndZipcode { get; set; }
            public string paymentIban { get; set; }
            public string ocrPaymentReference { get; set; }
            public string printDate { get; set; }
            public string dueDate { get; set; }
            public string notifiedDebt { get; set; }
            public string notifiedOverdueDebt { get; set; }
            public string totalDebt { get; set; }
            public string paymentBankGiroNr { get; set; }
            public List<IDictionary<string, object>> notifications { get; set; }
            public List<IDictionary<string, object>> overdueNotifications { get; set; }
            public IDictionary<string, object> unpaidNonOverdueAmountPerType { get; set; }
            public string notNotifiedCapitalDebt { get; set; }
            public bool? notNotifiedCapitalDebtNonZero { get; set; }
            public string mlObjectName { get; set; }
            public List<NotificationAmountTextPrintContextModel> amountTypes { get; set; }
            public List<NotificationAmountPrintContextModel> nonOverdueCustomCostAmountsList { get; set; }
        }

        public class TerminationLetterReceiverCustomerModel
        {
            public string CreditNr { get; set; }
            public int? ApplicantNr { get; set; }
            public int CustomerId { get; set; }
            public bool IsCompany { get; set; }
            public SharedCustomerPostalInfo PostalInfo { get; set; }
        }

        private IDictionary<string, List<TerminationLetterReceiverCustomerModel>> GetApplicantCustomerInfoByCreditNrs(ICreditContextExtended context, List<string> creditNrs, ICustomerPostalInfoRepository customerPostalInfoRepository, bool isForCompanyLoan, bool isForMortgageLoan)
        {
            var all = new List<TerminationLetterReceiverCustomerModel>();

            all.AddRange(context
                .CreditCustomersQueryable
                .Where(x => creditNrs.Contains(x.CreditNr))
                .Select(x => new TerminationLetterReceiverCustomerModel
                {
                    CreditNr = x.CreditNr,
                    CustomerId = x.CustomerId,
                    ApplicantNr = x.ApplicantNr,
                    IsCompany = isForCompanyLoan
                })
                .ToList());

            if (isForCompanyLoan)
            {
                var companyLoanCollateralCustomers = GetCompanyLoanCollateralCustomers(context, creditNrs);
                all.AddRange(companyLoanCollateralCustomers);
            }

            if (isForMortgageLoan)
            {
                var mortgageLoanCollateralCustomers = GetMortgageLoanCollateralCustomers(context, creditNrs, all);
                all.AddRange(mortgageLoanCollateralCustomers);
            }

            customerPostalInfoRepository.PreFetchCustomerPostalInfo(new HashSet<int>(all.Select(x => x.CustomerId)));

            var d = new Dictionary<string, List<TerminationLetterReceiverCustomerModel>>();

            foreach (var credit in all.GroupBy(x => x.CreditNr).Select(x => new { CreditNr = x.Key, Customers = x }))
            {
                foreach (var customer in credit.Customers.Distinct())
                {
                    var customerId = customer.CustomerId;
                    var applicantNr = customer.ApplicantNr;
                    customer.PostalInfo = customerPostalInfoRepository.GetCustomerPostalInfo(customerId);

                    if (!d.ContainsKey(credit.CreditNr))
                    {
                        d[credit.CreditNr] = new List<TerminationLetterReceiverCustomerModel>();
                    }

                    d[credit.CreditNr].Add(customer);
                }
            }

            return d;
        }

        private List<TerminationLetterReceiverCustomerModel> GetCompanyLoanCollateralCustomers(ICreditContextExtended context, List<string> creditNrs)
        {
            return context
                    .CreditHeadersQueryable
                    .Where(x => creditNrs.Contains(x.CreditNr))
                    .SelectMany(x => x.CustomerListMembers.Where(y => y.ListName == "companyLoanCollateral").Select(y => new { y.CustomerId, x.CreditNr }))
                    .ToList()
                    .Select(x => new TerminationLetterReceiverCustomerModel
                    {
                        ApplicantNr = null,
                        CustomerId = x.CustomerId,
                        CreditNr = x.CreditNr,
                        IsCompany = false
                    })
                    .ToList();
        }

        private List<TerminationLetterReceiverCustomerModel> GetMortgageLoanCollateralCustomers(ICreditContextExtended context, List<string> creditNrs, List<TerminationLetterReceiverCustomerModel> addedCustomers)
        {
            var addedCustomerIds = addedCustomers.Select(y => y.CustomerId).ToList();

            return context
                    .CreditHeadersQueryable
                    .Where(x => creditNrs.Contains(x.CreditNr))
                    .SelectMany(x => x.CustomerListMembers
                        .Where(y => !addedCustomerIds.Contains(y.CustomerId) && (y.ListName == "mortgageLoanPropertyOwner" || y.ListName == "mortgageLoanConsentingParty"))
                        .Select(y => new { y.CustomerId, x.CreditNr }))
                        .ToList()
                    .Select(x => new TerminationLetterReceiverCustomerModel
                    {
                        ApplicantNr = null,
                        CustomerId = x.CustomerId,
                        CreditNr = x.CreditNr,
                        IsCompany = false
                    })
                    .ToList();
        }

        private (List<CreditToBeTerminated>[] SingleCredits, List<CreditToBeTerminated>[] CoTerminationGroups) GroupForCoTermination(string[] creditNrs, ICreditContextExtended context)
        {
            var queryBase = context
                .CreditHeadersQueryable
                .Select(x => new
                {
                    x.CreditNr,
                    HasReminderDocuments = x.Reminders.Any(y => y.Documents.Any()),
                    HasMortgageLoanAgreementNr = x.DatedCreditStrings.Any(y => y.Name == DatedCreditStringCode.MortgageLoanAgreementNr.ToString()),
                    MortgageLoanAgreementNr = x.DatedCreditStrings.Where(y => y.Name == DatedCreditStringCode.MortgageLoanAgreementNr.ToString()).OrderByDescending(y => y.Id).Select(y => y.Value).FirstOrDefault()
                });
            var singleCreditNrs = queryBase.Where(x => creditNrs.Contains(x.CreditNr) && !x.HasMortgageLoanAgreementNr).Select(x => x.CreditNr).ToArray();

            List<CreditToBeTerminated>[] coTerminationGroups;
            if(singleCreditNrs.Length == creditNrs.Length)
            {
                coTerminationGroups = new List<CreditToBeTerminated>[] { };
            }
            else
            {
                var remainingCreditNrs = creditNrs.Except(singleCreditNrs).ToList();
                coTerminationGroups = queryBase
                    .Where(x => remainingCreditNrs.Contains(x.CreditNr))
                    .GroupBy(x => x.MortgageLoanAgreementNr)
                    .Select(x => x.Select(y => new CreditToBeTerminated
                    {
                        CreditNr = y.CreditNr,
                        IsCoNotificationMaster = y.HasReminderDocuments //tries to keep the same master as was used for reminders
                    }).ToList())
                    .ToArray();

                /*
                 Make sure there is exactly one master. Prefer the one that was used as master for reminders if there is one.
                 */
                foreach (var group in coTerminationGroups)
                {
                    if(group.Count(x => x.IsCoNotificationMaster == true) != 1)
                    {
                        var firstMasterIndex = group.FindIndex(x => x.IsCoNotificationMaster == true);
                        foreach (var letter in group)
                            letter.IsCoNotificationMaster = false;
                        if (firstMasterIndex >= 0)
                            group[firstMasterIndex].IsCoNotificationMaster = true;
                        else
                            group[0].IsCoNotificationMaster = true;
                    }
                }
            }

            return (
                SingleCredits: singleCreditNrs.Select(x => new List<CreditToBeTerminated> { new CreditToBeTerminated { CreditNr = x } }).ToArray(),
                CoTerminationGroups: coTerminationGroups);
        }

        private class CoTerminationContext
        {
            private readonly List<CreditToBeTerminated> credits;
            private Dictionary<int, CustomerDataModel> PerCustomerData = new Dictionary<int, CustomerDataModel>();
            public string CoTerminationId { get; set; } = Guid.NewGuid().ToString();

            public CoTerminationContext(List<CreditToBeTerminated> credits)
            {
                this.credits = credits;
            }

            //Make sure the master is handled last so it has access to the data and print context for all.
            public IEnumerable<CreditToBeTerminated> GetHandlingOrder() =>
                credits.OrderBy(x => x.IsCoNotificationMaster == true ? 1 : 0);

            public bool IsCoTerminationMaster(string creditNr) => credits.Single(x => x.CreditNr == creditNr).IsCoNotificationMaster == true;

            internal void AddTermination(TerminationLetterPrintContext printContext, TerminationLetterReceiverCustomerModel customer, decimal notifiedOverdueDebt, DateTime dueDate, decimal totalDebt)
            {
                if (!PerCustomerData.ContainsKey(customer.CustomerId))
                    PerCustomerData[customer.CustomerId] = new CustomerDataModel();

                var customerData = PerCustomerData[customer.CustomerId];
                customerData.PrintContexts.Add(printContext);
                customerData.DueDates.Add(dueDate);
                customerData.NotifiedOverdueDebt += notifiedOverdueDebt;
                customerData.TotalDebt += totalDebt;
            }

            internal decimal NotifiedOverdueDebt(int customerId) => PerCustomerData[customerId].NotifiedOverdueDebt;
            internal DateTime MinDueDate(int customerId) => PerCustomerData[customerId].DueDates.Min();

            internal List<TerminationLetterPrintContext> PrintContexts(int customerId) => PerCustomerData[customerId].PrintContexts;

            internal decimal TotalDebt(int customerId) => PerCustomerData[customerId].TotalDebt;

            private class CustomerDataModel
            {
                public List<TerminationLetterPrintContext> PrintContexts = new List<TerminationLetterPrintContext>();
                public List<DateTime> DueDates { get; set; } = new List<DateTime>();
                public decimal NotifiedOverdueDebt { get; set; } = 0m;
                public decimal TotalDebt { get; set; } = 0m;
            }
        }

        private class CreditToBeTerminated
        {
            public string CreditNr { get; set; }
            public bool? IsCoNotificationMaster { get; set; }
        }
    }
}