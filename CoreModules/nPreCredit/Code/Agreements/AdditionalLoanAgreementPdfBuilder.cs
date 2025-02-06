using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NTech;
using NTech.Banking.LoanModel;
using NTech.Core.Module;
using Serilog;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace nPreCredit.Code
{
    public class AdditionalLoanAgreementPdfBuilder : ILoanAgreementPdfBuilder
    {
        private readonly IClock clock;
        private readonly IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository;

        public AdditionalLoanAgreementPdfBuilder(IClock clock, IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository)
        {
            this.clock = clock;
            this.partialCreditApplicationModelRepository = partialCreditApplicationModelRepository;
        }

        private static List<string> S(params string[] names)
        {
            return new List<string>(names);
        }

        private string GetTranslation(string lang, string key)
        {
            var tr = Translations.GetTranslationTable();
            if (!tr.ContainsKey(lang))
                return null;
            var tl = tr[lang];
            if (!tl.ContainsKey(key))
                return null;
            return tl[key];
        }

        public bool IsCreateAgreementPdfAllowed(string applicationNr, out string reasonMessage)
        {
            var appModel = partialCreditApplicationModelRepository.Get(applicationNr, applicantFields: new List<string> { "customerId" });
            var customerClient = new PreCreditCustomerClient();

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
                        "civicRegNr",
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
                NEnv.ClientCfg.Country.BaseCountry == "SE" ? "bankaccountnr" : "iban" }))
            {
                reasonMessage = "Missing from application: " + missingFieldsMessage;
                return false;
            }

            using (var db = new PreCreditContext())
            {
                var ah = db.CreditApplicationHeaders.Where(x => x.ApplicationNr == applicationNr).Select(x => new
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

                var d = CreditDecisionModelParser.ParseAcceptedAdditionalLoanOffer(cd.AcceptedDecisionModel);
                if (d == null || !d.amount.HasValue || string.IsNullOrWhiteSpace(d.creditNr))
                {
                    reasonMessage = "Accepted additional loan decision must have an offer";
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

                pdfBytes = CreateAdditionalLoanAgreementPdfI(applicationNr, observeAgreementDataHash);
                errorMessage = null;
                return true;
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Failed to create loan agreement on application {applicationNr}", applicationNr);
                pdfBytes = null;
                errorMessage = ex.Message;
                return false;
            }
        }

        private byte[] CreateAdditionalLoanAgreementPdfI(string applicationNr, Action<string> observeAgreementDataHash)
        {
            if (NEnv.ClientCfg.Country.BaseCountry != "FI")
            {
                throw new NotImplementedException();
            }

            const string language = "fi";
            using (var c = new PreCreditContext())
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
                    applicationFields: S("iban")
                    );
                var ch = c.CreditApplicationHeaders.Where(x => x.ApplicationNr == applicationNr).Select(x => new { x.CurrentCreditDecision, x.ProviderName }).Single();
                var providerName = ch.ProviderName;

                var creditDecision = ch.CurrentCreditDecision as AcceptedCreditDecision;
                if (creditDecision == null)
                    throw new Exception("Application has no approved credit decision");

                var offer = CreditDecisionModelParser.ParseAcceptedAdditionalLoanOffer(creditDecision.AcceptedDecisionModel);

                if (offer == null)
                    throw new Exception("Missing offer on Credit decision");

                if (offer.amount.GetValueOrDefault() <= 0m)
                    throw new Exception("Missing amount on Credit decision");
                if (string.IsNullOrWhiteSpace(offer.creditNr))
                    throw new Exception("Missing creditNr on Credit decision");
                //Affiliate
                var affiliateModel = NEnv.GetAffiliateModel(providerName);

                var creditClient = new CreditClient();
                var credit = creditClient.GetCustomerCreditHistoryByCreditNrs(new List<string>() { offer.creditNr }).Single();

                //Compute loan terms
                var totalCapitalAmount = offer.amount.Value + credit.CapitalBalance;
                var totalInterestRatePercent = (offer.newMarginInterestRatePercent ?? credit.MarginInterestRatePercent).Value + credit.ReferenceInterestRatePercent.Value;

                var t = PaymentPlanCalculation.BeginCreateWithAnnuity(
                    totalCapitalAmount,
                    (offer.newAnnuityAmount ?? credit.AnnuityAmount).Value,
                    totalInterestRatePercent,
                    null,
                    NEnv.CreditsUse360DayInterestYear);

                if (credit.NotificationFeeAmount.HasValue)
                    t = t.WithMonthlyFee(credit.NotificationFeeAmount.Value);

                var terms = t.EndCreate();

                var effectiveInterestRatePercent = terms.EffectiveInterestRatePercent.Value;
                var annuityAmount = terms.AnnuityAmount;
                var totalPaidAmount = terms.TotalPaidAmount;
                var initialPaidToCustomerAmount = terms.InitialPaidToCustomerAmount;

                var f = CultureInfo.GetCultureInfo(NEnv.ClientCfg.Country.BaseFormattingCulture);

                var m = new LoanAgreementViewModel
                {
                    agreementDate = this.clock.Now.ToString("dd.MM.yyyy"),
                    affiliate = affiliateModel,
                    loanNumber = offer.creditNr,
                    additionalLoanAmount = offer.amount.Value.ToString("C", f),
                    currentLoanAmount = credit.CapitalBalance.ToString("C", f),
                    totalLoanAmount = totalCapitalAmount.ToString("C", f),
                    repaymentTimeInMonths = terms.Payments.Count.ToString(f),
                    monthlyPayment = (terms.AnnuityAmount + credit.NotificationFeeAmount.GetValueOrDefault()).ToString("C", f),
                    marginInterestRate = ((offer.newMarginInterestRatePercent ?? credit.MarginInterestRatePercent).Value / 100m).ToString("P", f),
                    referenceInterestRate = (credit.ReferenceInterestRatePercent.Value / 100m).ToString("P", f),
                    totalInterestRate = (totalInterestRatePercent / 100m).ToString("P", f),
                    effectiveInterestRate = (terms.EffectiveInterestRatePercent.Value / 100m).ToString("P", f),
                    notificationFee = (offer.newNotificationFeeAmount ?? credit.NotificationFeeAmount).GetValueOrDefault().ToString("C", f),
                    totalPaidAmount = terms.TotalPaidAmount.ToString("C", f),
                    withdrawalAmount = offer.amount.Value.ToString("C", f),
                    with_interestceiling = NEnv.LegalInterestCeilingPercent.HasValue ? "true" : null,
                    without_interestceiling = NEnv.LegalInterestCeilingPercent.HasValue ? null : "true"
                };

                m.iban = appModel.Application.Get("iban").StringValue.Required;

                m.sekki = new Sekki
                {
                    sekkiAffiliate = affiliateModel.IsSelf ? null : affiliateModel,
                    loanAmount = totalCapitalAmount.ToString("N2", f),
                    repaymentTimeInMonths = terms.Payments.Count.ToString(f),
                    nrOfPayments = terms.Payments.Count.ToString(f),
                    monthlyPayment = (terms.AnnuityAmount + credit.NotificationFeeAmount.GetValueOrDefault()).ToString("N2", f),
                    totalCostAmount = (terms.TotalPaidAmount - totalCapitalAmount).ToString("N2", f),
                    totalPaidAmount = terms.TotalPaidAmount.ToString("N2", f),
                    effectiveInterestRate = (terms.EffectiveInterestRatePercent.Value / 100m).ToString("P", f),
                    initialFee = 0m.ToString("N2", f),
                    notificationFee = credit.NotificationFeeAmount.GetValueOrDefault().ToString("N2", f),
                    totalInterestRate = (totalInterestRatePercent / 100m).ToString("P", f)
                };

                Func<string, string> formatMonth = s =>
                {
                    if (string.IsNullOrWhiteSpace(s))
                        return null;

                    DateTime d;

                    if (!DateTime.TryParseExact(s + "-01", "yyyy-MM-dd", f, DateTimeStyles.None, out d))
                    {
                        NLog.Warning("Malformed employed since date on application {applicationNr}", applicationNr);
                        return null;
                    }
                    return d.ToString("MM.yyyy");
                };

                Func<string, string> formatMarriage = s =>
                {
                    return GetTranslation(language, s) ?? s;
                };

                var customerClient = new PreCreditCustomerClient();
                int customerId;

                foreach (var applicantNr in Enumerable.Range(1, appModel.NrOfApplicants))
                {
                    var am = appModel.Applicant(applicantNr);
                    if (int.TryParse(am.Get("customerId").StringValue.Required, out customerId))
                    {
                        var items = customerClient.GetCustomerCardItems(customerId, "firstName", "lastName", "addressStreet", "addressZipcode", "addressCity", "addressCountry");
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
                            var educationTranslation = GetTranslation("fi", education);
                            a.education = educationTranslation ?? education;
                        }

                        var employment = am.Get("employment").StringValue.Optional;
                        if (employment != null)
                        {
                            var employmentTranslation = GetTranslation("fi", employment);
                            a.employment = employmentTranslation ?? employment;
                        }

                        var housing = am.Get("housing").StringValue.Optional;
                        if (housing != null)
                        {
                            var housingTranslation = GetTranslation("fi", housing);
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
                }

                var context = JsonConvert.DeserializeObject<ExpandoObject>(JsonConvert.SerializeObject(m), new ExpandoObjectConverter()) as IDictionary<string, object>;

                if (observeAgreementDataHash != null)
                {
                    observeAgreementDataHash(CreateHash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(context))));
                }

                var dc = new nDocumentClient();
                var pdfBytes = dc.PdfRenderDirect(
                    "credit-agreement-additionalloan",
                    context);
                return pdfBytes;
            }
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
            public string additionalLoanAmount { get; set; }
            public string currentLoanAmount { get; set; }
            public string totalLoanAmount { get; set; }
            public string repaymentTimeInMonths { get; set; }
            public string monthlyPayment { get; set; }
            public string marginInterestRate { get; set; }
            public string referenceInterestRate { get; set; }
            public string totalInterestRate { get; set; }
            public string effectiveInterestRate { get; set; }
            public string notificationFee { get; set; }
            public string totalPaidAmount { get; set; }
            public AffiliateModel affiliate { get; set; }
            public string iban { get; set; }
            public string withdrawalAmount { get; set; }
            public Sekki sekki { get; set; }
            public string with_interestceiling { get; set; }
            public string without_interestceiling { get; set; }
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