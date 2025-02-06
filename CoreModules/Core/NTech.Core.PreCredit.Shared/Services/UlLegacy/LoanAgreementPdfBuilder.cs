using Newtonsoft.Json;
using NTech.Banking.LoanModel;
using NTech.Core;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Core.PreCredit.Shared;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using CoreDocumentClient = NTech.Core.Module.Shared.Clients.IDocumentClient;

namespace nPreCredit.Code
{
    public class LoanAgreementPdfBuilder : ILoanAgreementPdfBuilder
    {
        private ICoreClock clock;
        private readonly IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository;
        private readonly ICustomerClient customerClient;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly PreCreditContextFactory contextFactory;
        private readonly ILoggingService loggingService;
        private readonly IPreCreditEnvSettings envSettings;
        private readonly CoreDocumentClient documentClient;
        private readonly Dictionary<string, string> finnishTranslations;
        private readonly Func<string, byte[]> loadPdfTemplate;

        public LoanAgreementPdfBuilder(ICoreClock clock, IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository,
            ICustomerClient customerClient, IClientConfigurationCore clientConfiguration, PreCreditContextFactory contextFactory,
            ILoggingService loggingService, IPreCreditEnvSettings envSettings, CoreDocumentClient documentClient,
            Dictionary<string, string> finnishTranslations, Func<string, byte[]> loadPdfTemplate)
        {
            this.clock = clock;
            this.partialCreditApplicationModelRepository = partialCreditApplicationModelRepository;
            this.customerClient = customerClient;
            this.clientConfiguration = clientConfiguration;
            this.contextFactory = contextFactory;
            this.loggingService = loggingService;
            this.envSettings = envSettings;
            this.documentClient = documentClient;
            this.finnishTranslations = finnishTranslations;
            this.loadPdfTemplate = loadPdfTemplate;
        }

        private static List<string> S(params string[] names)
        {
            return new List<string>(names);
        }

        private string GetFinnishTranslation(string key)
        {
            if (!finnishTranslations.ContainsKey(key))
                return null;
            return finnishTranslations[key];
        }

        public bool IsCreateAgreementPdfAllowed(string applicationNr, out string reasonMessage)
        {
            var appModel = partialCreditApplicationModelRepository.Get(applicationNr, applicantFields: new List<string> { "customerId" });

            string customerIssuesMessage = "";
            appModel.DoForEachApplicant(applicantNr =>
            {
                var cid = appModel.Applicant(applicantNr).Get("customerId").IntValue.Optional;

                if (!cid.HasValue)
                {
                    customerIssuesMessage += $" Applicant {applicantNr}: Missing customerId";
                }
                else
                {
                    var issues = customerClient.CheckPropertyStatus(cid.Value, new HashSet<string>
                    {
                        "firstName",
                        "addressZipcode",
                        "civicRegNr"
                    });
                    if (issues.MissingPropertyNames?.Any() ?? false)
                    {
                        customerIssuesMessage += $" Applicant {applicantNr}: {issues.GetMissingPropertyNamesIssueDescription()}";
                    }
                }
            });

            if (customerIssuesMessage.Length > 0)
            {
                reasonMessage = customerIssuesMessage;
                return false;
            }

            string missingFieldsMessage;
            if (!partialCreditApplicationModelRepository.ExistsAll(applicationNr, out missingFieldsMessage, applicationFields: new List<string> {
                clientConfiguration.Country.BaseCountry == "SE" ? "bankaccountnr" : "iban" }))
            {
                reasonMessage = "Missing from application: " + missingFieldsMessage;
                return false;
            }

            using (var db = contextFactory.CreateContext())
            {
                var ah = db.CreditApplicationHeadersQueryable.Where(x => x.ApplicationNr == applicationNr).Select(x => new
                {
                    x.CurrentCreditDecision,
                    x.CreditCheckStatus,
                    x.CustomerCheckStatus
                }).Single();

                if (ah.CreditCheckStatus != "Accepted")
                {
                    reasonMessage = "Credit check must be ok";
                    return false;
                }

                if (ah.CustomerCheckStatus == "Rejected")
                {
                    reasonMessage = "Customer check must not be rejected";
                    return false;
                }

                var cd = ah.CurrentCreditDecision as AcceptedCreditDecision;

                if (cd == null)
                {
                    reasonMessage = "Must have an accepted credit decision";
                    return false;
                }

                var d = CreditDecisionModelParser.ParseAcceptedNewCreditOffer(cd.AcceptedDecisionModel);
                if (d == null || d == null || !d.amount.HasValue)
                {
                    reasonMessage = "Accepted credit decision must have an offer";
                    return false;
                }
            }

            reasonMessage = null;
            return true;
        }

        public bool TryCreateAgreementPdf(out byte[] pdfBytes, out string errorMessage, string applicationNr, bool skipAllowedCheck = false, Action<string> observeAgreementDataHash = null)
        {
            try
            {
                if (!skipAllowedCheck)
                {
                    string notAllowedMessage;
                    if (!IsCreateAgreementPdfAllowed(applicationNr, out notAllowedMessage))
                    {
                        errorMessage = "Preconditions not met: " + notAllowedMessage;
                        pdfBytes = null;
                        return false;
                    }
                }

                pdfBytes = CreateNewLoanAgreementPdfI(applicationNr, observeAgreementDataHash);
                errorMessage = null;
                return true;
            }
            catch (Exception ex)
            {
                loggingService.Error(ex, $"Failed to create loan agreement on application {applicationNr}");
                pdfBytes = null;
                errorMessage = ex.Message;
                return false;
            }
        }

        public IDictionary<string, object> CreateNewLoanAgreementPrintContext(string applicationNr)
        {
            using (var c = contextFactory.CreateContext())
            {
                //Get the application
                var appModel = partialCreditApplicationModelRepository.Get(
                    applicationNr,
                    applicantFields: S(
                        "customerId",
                        "civicRegNr",
                        "education",
                        "housing",
                        "housingCostPerMonthAmount",
                        "employment",
                        "employedSinceMonth",
                        "incomePerMonthAmount",
                        "marriage",
                        "nrOfChildren",
                        "studentLoanAmount",
                        "studentLoanCostPerMonthAmount",
                        "carOrBoatLoanAmount",
                        "carOrBoatLoanCostPerMonthAmount",
                        "mortgageLoanAmount",
                        "mortgageLoanCostPerMonthAmount",
                        "otherLoanAmount",
                        "otherLoanCostPerMonthAmount",
                        "creditCardAmount",
                        "creditCardCostPerMonthAmount",
                        "employer",
                        "employerPhone",
                        "email",
                        "phone"
                        ),
                    applicationFields: S(
                        clientConfiguration.Country.BaseCountry == "SE" ? "bankaccountnr" : "iban",
                        "creditnr"
                        )
                    );
                var ch = c
                    .CreditApplicationHeadersQueryable
                    .Where(x => x.ApplicationNr == applicationNr).Select(x => new { x.CurrentCreditDecision, x.ProviderName })
                    .Single();
                var providerName = ch.ProviderName;

                var creditDecision = ch.CurrentCreditDecision as AcceptedCreditDecision;
                if (creditDecision == null)
                    throw new Exception("Application has no approved credit decision");

                var offer = CreditDecisionModelParser.ParseAcceptedNewCreditOffer(creditDecision.AcceptedDecisionModel);

                if (offer == null)
                    throw new Exception("Missing offer on Credit decision");
                if (offer.amount.GetValueOrDefault() <= 0m)
                    throw new Exception("Missing amount on Credit decision");
                if (offer.repaymentTimeInMonths.GetValueOrDefault() <= 0m)
                    throw new Exception("Missing repaymentTimeInMonths on Credit decision");
                if (offer.marginInterestRatePercent.GetValueOrDefault() <= 0m)
                    throw new Exception("Missing marginInterestRatePercent on Credit decision");
                if (!offer.referenceInterestRatePercent.HasValue)
                    throw new Exception("Missing referenceInterestRatePercent on Credit decision");

                //Affiliate
                var affiliateModel = envSettings.GetAffiliateModel(providerName);

                //Compute loan terms
                decimal? effectiveInterestRatePercent = offer.effectiveInterestRatePercent;
                decimal? annuityAmount = offer.annuityAmount;
                decimal? totalPaidAmount = offer.totalPaidAmount;
                decimal? initialPaidToCustomerAmount = offer.initialPaidToCustomerAmount;
                if (!effectiveInterestRatePercent.HasValue || !annuityAmount.HasValue || !totalPaidAmount.HasValue || !initialPaidToCustomerAmount.HasValue)
                {
                    var terms = PaymentPlanCalculation
                        .BeginCreateWithRepaymentTime(offer.amount.Value, offer.repaymentTimeInMonths.Value, offer.marginInterestRatePercent.Value + offer.referenceInterestRatePercent.Value, true, null, envSettings.CreditsUse360DayInterestYear)
                        .WithInitialFeeCapitalized(offer.initialFeeAmount.Value)
                        .WithMonthlyFee(offer.notificationFeeAmount.Value)
                        .EndCreate();
                    if (!terms.EffectiveInterestRatePercent.HasValue)
                        throw new Exception("Effective interest rate missing from terms");

                    effectiveInterestRatePercent = terms.EffectiveInterestRatePercent.Value;
                    annuityAmount = terms.AnnuityAmount;
                    totalPaidAmount = terms.TotalPaidAmount;
                    initialPaidToCustomerAmount = terms.InitialPaidToCustomerAmount;
                }

                var f = CultureInfo.GetCultureInfo(clientConfiguration.Country.BaseFormattingCulture);

                var m = new LoanAgreementViewModel
                {
                    agreementDate = this.clock.Now.ToString("dd.MM.yyyy"),
                    affiliate = affiliateModel,
                    loanNumber = appModel.Application.Get("creditnr").StringValue.Required,
                    loanAmount = offer.amount.Value.ToString("C", f),
                    repaymentTimeInMonths = offer.repaymentTimeInMonths.Value.ToString(f),
                    monthlyPayment = (annuityAmount.Value + offer.notificationFeeAmount.Value).ToString("C", f),
                    marginInterestRate = (offer.marginInterestRatePercent.Value / 100m).ToString("P", f),
                    referenceInterestRate = (offer.referenceInterestRatePercent.Value / 100m).ToString("P", f),
                    totalInterestRate = ((offer.marginInterestRatePercent.Value + offer.referenceInterestRatePercent.Value) / 100m).ToString("P", f),
                    effectiveInterestRate = (effectiveInterestRatePercent.Value / 100m).ToString("P", f),
                    notificationFee = offer.notificationFeeAmount.Value.ToString("C", f),
                    initialFee = offer.initialFeeAmount.Value.ToString("C", f),
                    totalPaidAmount = totalPaidAmount.Value.ToString("C", f),
                    withdrawalAmount = initialPaidToCustomerAmount.Value.ToString("C", f),
                    with_interestceiling = "true",
                    without_interestceiling = null
                };

                m.iban = appModel.Application.Get("iban").StringValue.Required;

                m.sekki = new Sekki
                {
                    sekkiAffiliate = affiliateModel.IsSelf ? null : affiliateModel,
                    loanAmount = offer.amount.Value.ToString("N2", f),
                    repaymentTimeInMonths = offer.repaymentTimeInMonths.Value.ToString(f),
                    nrOfPayments = offer.repaymentTimeInMonths.Value.ToString(f),
                    monthlyPayment = (annuityAmount.Value + offer.notificationFeeAmount.Value).ToString("N2", f),
                    totalCostAmount = (totalPaidAmount.Value - offer.amount.Value).ToString("N2", f),
                    totalPaidAmount = totalPaidAmount.Value.ToString("N2", f),
                    effectiveInterestRate = (effectiveInterestRatePercent.Value / 100m).ToString("P", f),
                    initialFee = offer.initialFeeAmount.Value.ToString("N2", f),
                    notificationFee = offer.notificationFeeAmount.Value.ToString("N2", f),
                    totalInterestRate = ((offer.marginInterestRatePercent.Value + offer.referenceInterestRatePercent.Value) / 100m).ToString("P", f)
                };

                Func<string, string> formatMonth = s =>
                {
                    if (string.IsNullOrWhiteSpace(s))
                        return null;

                    DateTime d;

                    if (!DateTime.TryParseExact(s + "-01", "yyyy-MM-dd", f, DateTimeStyles.None, out d))
                    {
                        loggingService.Warning($"Malformed employed since date on application {applicationNr}");
                        return null;
                    }
                    return d.ToString("MM.yyyy");
                };

                Func<string, string> formatMarriage = s =>
                {
                    return GetFinnishTranslation(s) ?? s;
                };

                var allCustomerIds = Enumerable
                    .Range(1, appModel.NrOfApplicants)
                    .Select(x => int.Parse(appModel.Applicant(x).Get("customerId").StringValue.Required))
                    .ToHashSetShared();
                var customerItemsByCustomerId = customerClient
                            .BulkFetchPropertiesByCustomerIdsD(allCustomerIds, "firstName", "lastName", "addressStreet", "addressZipcode",
                            "addressCity", "addressCountry");

                int customerId;
                foreach (var applicantNr in Enumerable.Range(1, appModel.NrOfApplicants))
                {
                    var am = appModel.Applicant(applicantNr);
                    if (!int.TryParse(am.Get("customerId").StringValue.Required, out customerId))
                        throw new Exception($"Broken application, missing customerId: {applicationNr}");

                    var items = customerItemsByCustomerId
                        .Opt(customerId);
                    var a = new Applicant();

                    a.fullName = ((items.Opt("firstName") ?? "") + " " + (items.Opt("lastName") ?? "")).Trim(); ;
                    a.streetAddress = items.Opt("addressStreet");
                    a.areaAndZipcode = ((items.Opt("addressZipcode") ?? "") + " " + (items.Opt("addressCity") ?? "")).Trim();
                    a.phone = am.Get("phone").StringValue.Optional;
                    a.email = am.Get("email").StringValue.Optional;

                    a.civicRegNr = am.Get("civicRegNr").StringValue.Required;

                    var education = am.Get("education").StringValue.Optional;
                    if (education != null)
                    {
                        var educationTranslation = GetFinnishTranslation(education);
                        a.education = educationTranslation ?? education;
                    }

                    var employment = am.Get("employment").StringValue.Optional;
                    if (employment != null)
                    {
                        var employmentTranslation = GetFinnishTranslation(employment);
                        a.employment = employmentTranslation ?? employment;
                    }

                    var housing = am.Get("housing").StringValue.Optional;
                    if (housing != null)
                    {
                        var housingTranslation = GetFinnishTranslation(housing);
                        a.housing = housingTranslation ?? housing;
                    }

                    a.housingMonthlyCost = am.Get("housingCostPerMonthAmount").DecimalValue.Optional?.ToString("C", f);
                    a.employedSince = formatMonth(am.Get("employedSinceMonth").StringValue.Optional);
                    a.employer = am.Get("employer").StringValue.Optional;
                    a.employerPhone = am.Get("employerPhone").StringValue.Optional;
                    a.monthlyIncome = am.Get("incomePerMonthAmount").DecimalValue.Optional?.ToString("C", f);
                    a.marriageStatus = formatMarriage(am.Get("marriage").StringValue.OptionalOneOf("marriage_gift", "marriage_ogift", "marriage_sambo"));
                    a.nrOfChildren = am.Get("nrOfChildren").IntValue.Optional?.ToString(f);

                    a.studentLoanAmount = am.Get("studentLoanAmount").DecimalValue.Optional?.ToString("C", f);
                    a.studentLoanCostPerMonthAmount = am.Get("studentLoanCostPerMonthAmount").DecimalValue.Optional?.ToString("C", f);
                    a.carOrBoatLoanAmount = am.Get("carOrBoatLoanAmount").DecimalValue.Optional?.ToString("C", f);
                    a.carOrBoatLoanCostPerMonthAmount = am.Get("carOrBoatLoanCostPerMonthAmount").DecimalValue.Optional?.ToString("C", f);
                    a.mortgageLoanAmount = am.Get("mortgageLoanAmount").DecimalValue.Optional?.ToString("C", f);
                    a.mortgageLoanCostPerMonthAmount = am.Get("mortgageLoanCostPerMonthAmount").DecimalValue.Optional?.ToString("C", f);
                    a.otherLoanAmount = am.Get("otherLoanAmount").DecimalValue.Optional?.ToString("C", f);
                    a.otherLoanCostPerMonthAmount = am.Get("otherLoanCostPerMonthAmount").DecimalValue.Optional?.ToString("C", f);
                    a.creditCardAmount = am.Get("creditCardAmount").DecimalValue.Optional?.ToString("C", f);
                    a.creditCardCostPerMonthAmount = am.Get("creditCardCostPerMonthAmount").DecimalValue.Optional?.ToString("C", f);

                    if (applicantNr == 1)
                        m.applicant1 = a;
                    else if (applicantNr == 2)
                        m.applicant2 = a;
                    else
                        throw new NotImplementedException();

                }

                return PdfCreator.ToTemplateContext(m);
            }
        }

        private byte[] CreateNewLoanAgreementPdfI(string applicationNr, Action<string> observeAgreementDataHash)
        {
            var context = CreateNewLoanAgreementPrintContext(applicationNr);
            if (observeAgreementDataHash != null)
            {
                observeAgreementDataHash(CreateHash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(context))));
            }
            var pdfBytes = documentClient.PdfRenderDirect(
                this.loadPdfTemplate("credit-agreement"),
                context);
            return pdfBytes;
        }

        private static string CreateHash(byte[] inputBytes)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                return Convert.ToBase64String(md5.ComputeHash(inputBytes));
            }
        }

        public class LoanAgreementViewModel
        {
            public string agreementDate { get; set; }
            public Applicant applicant1 { get; set; }
            public Applicant applicant2 { get; set; }
            public string loanNumber { get; set; }
            public string loanAmount { get; set; }
            public string repaymentTimeInMonths { get; set; }
            public string monthlyPayment { get; set; }
            public string marginInterestRate { get; set; }
            public string referenceInterestRate { get; set; }
            public string totalInterestRate { get; set; }
            public string effectiveInterestRate { get; set; }
            public string notificationFee { get; set; }
            public string initialFee { get; set; }
            public string totalPaidAmount { get; set; }
            public AffiliateModel affiliate { get; set; }
            public string iban { get; set; }
            public BankAccount bankAccount { get; set; }
            public string withdrawalAmount { get; set; }
            public Sekki sekki { get; set; }
            public string with_interestceiling { get; set; }
            public string without_interestceiling { get; set; }

            public class BankAccount
            {
                public string bankName { get; set; }
                public string accountNr { get; set; }
                public string clearingNr { get; set; }
            }
        }

        public class Applicant
        {
            public string fullName { get; set; }
            public string civicRegNr { get; set; }
            public string streetAddress { get; set; }
            public string areaAndZipcode { get; set; }
            public string phone { get; set; }
            public string email { get; set; }
            public string education { get; set; }
            public string housing { get; set; }
            public string housingMonthlyCost { get; set; }
            public string employment { get; set; }
            public string employer { get; set; }
            public string employerPhone { get; set; }
            public string employedSince { get; set; }
            public string monthlyIncome { get; set; }
            public string marriageStatus { get; set; }
            public string nrOfChildren { get; set; }
            public string studentLoanAmount { get; set; }
            public string studentLoanCostPerMonthAmount { get; set; }
            public string mortgageLoanAmount { get; set; }
            public string mortgageLoanCostPerMonthAmount { get; set; }
            public string carOrBoatLoanAmount { get; set; }
            public string carOrBoatLoanCostPerMonthAmount { get; set; }
            public string otherLoanAmount { get; set; }
            public string otherLoanCostPerMonthAmount { get; set; }
            public string creditCardAmount { get; set; }
            public string creditCardCostPerMonthAmount { get; set; }
        }

        public class Sekki
        {
            public string loanAmount { get; set; }
            public string repaymentTimeInMonths { get; set; }
            public string nrOfPayments { get; set; }
            public string monthlyPayment { get; set; }
            public string totalCostAmount { get; set; }
            public string totalPaidAmount { get; set; }
            public string totalInterestRate { get; set; }
            public string effectiveInterestRate { get; set; }
            public string initialFee { get; set; }
            public string notificationFee { get; set; }
            public AffiliateModel sekkiAffiliate { get; set; }
        }
    }
}