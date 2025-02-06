using Newtonsoft.Json;
using nPreCredit.Code.Datasources;
using NTech;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using static nPreCredit.Code.Services.MortgageLoans.MortgageLoanDualApplicationAndPoaPrintContext;

namespace nPreCredit.Code.Services.MortgageLoans
{
    public class MortgageLoanDualApplicationAndPoaService : IMortgageLoanDualApplicationAndPoaService
    {
        private readonly ApplicationDataSourceService applicationDataSourceService;
        private readonly IClock clock;
        private readonly IKeyValueStoreService keyValueStoreService;
        private readonly ICreditApplicationCustomEditableFieldsService customFieldsService;
        private readonly Lazy<CultureInfo> formattingCulture;

        public MortgageLoanDualApplicationAndPoaService(
                  ApplicationDataSourceService applicationDataSourceService, IClientConfiguration clientConfiguration, IClock clock, IKeyValueStoreService keyValueStoreService,
                  ICreditApplicationCustomEditableFieldsService customFieldsService)

        {
            this.applicationDataSourceService = applicationDataSourceService;
            this.clock = clock;
            this.formattingCulture = new Lazy<CultureInfo>(() => CultureInfo.GetCultureInfo(clientConfiguration.Country.BaseFormattingCulture));
            this.keyValueStoreService = keyValueStoreService;
            this.customFieldsService = customFieldsService;
        }

        private void EnsureLoansRequested(Dictionary<string, HashSet<string>> requestedData)
        {
            if (!requestedData.ContainsKey("ComplexApplicationList"))
                requestedData["ComplexApplicationList"] = new HashSet<string>();

            foreach (var name in Enumerables.Array("loanApplicant1IsParty", "loanApplicant2IsParty", "bankName", "loanShouldBeSettled"))
            {
                requestedData["ComplexApplicationList"].Add($"CurrentMortgageLoans#*#u#{name}");
            }

            foreach (var name in Enumerables.Array("loanApplicant1IsParty", "loanApplicant2IsParty", "bankName", "loanShouldBeSettled"))
            {
                requestedData["ComplexApplicationList"].Add($"CurrentOtherLoans#*#u#{name}");
            }
        }

        public bool TryGetPrintContext(string applicationNr, int applicantNo, bool onlyApplication, string onlyPoaForBankName, out MortgageLoanDualApplicationAndPoaPrintContext context, out string failedMessage)
        {
            if (onlyApplication && onlyPoaForBankName != null)
                throw new Exception("onlyApplication and onlyPoaForBankName cannot be combined");

            var ai = applicationDataSourceService.GetDataSimple(applicationNr, new Dictionary<string, HashSet<string>>
            {
                { "CreditApplicationInfo", new HashSet<string> { "NrOfApplicants" } }
            });

            var nrOfApplicants = ai.Item("CreditApplicationInfo", "NrOfApplicants").IntValue.Required;
            var applicantNrs = Enumerable.Range(1, nrOfApplicants);

            var requestedData = new Dictionary<string, HashSet<string>>
            {
                { "CustomerCardItem", new HashSet<string>() },
                { "CreditApplicationItem", new HashSet<string>() },
                 { "ComplexApplicationList", new HashSet<string>() }
             };

            foreach (var applicantNr in Enumerable.Range(1, nrOfApplicants))
            {
                foreach (var name in Enumerables.Array("civicRegNr", "firstName", "lastName", "addressStreet", "addressZipcode", "addressCity", "phone", "email"))
                {
                    requestedData["CustomerCardItem"].Add($"a{applicantNr}.{name}");
                }

                foreach (var name in Enumerables.Array("employment", "childrenMinorCount", "isFirstTimeBuyer", "employedSince", "employer", "profession", "employedTo", "marriage", "monthlyIncomeSalaryAmount", "monthlyIncomePensionAmount", "monthlyIncomeCapitalAmount", "monthlyIncomeBenefitsAmount",
                    "monthlyIncomeOtherAmount", "childrenMinorCount", "childrenAdultCount", "costOfLivingRent", "costOfLivingFees"))
                {
                    requestedData["CreditApplicationItem"].Add($"applicant{applicantNr}.{name}");
                }
            }

            EnsureLoansRequested(requestedData);

            var questionsRaw = keyValueStoreService.GetValue(applicationNr, "additionalQuestionsDocument");
            AdditionalQuestionsDocumentModel questions = questionsRaw == null ? null : JsonConvert.DeserializeObject<AdditionalQuestionsDocumentModel>(questionsRaw);

            var data = applicationDataSourceService.GetDataSimple(applicationNr, requestedData);

            Func<string, string> formatNull = x => string.IsNullOrWhiteSpace(x) ? "-" : x;

            var customerCard = data.DataSource("CustomerCardItem");
            var ci = data.DataSource("CreditApplicationItem");

            Func<int, ApplicationContactInfo> getContactInfoByApplicantNr = n => new ApplicationContactInfo
            {
                fullName = formatNull(customerCard[$"a{n}.firstName"].StringValue.Optional + " " + customerCard[$"a{n}.lastName"].StringValue.Optional),
                civicRegNr = formatNull(customerCard[$"a{n}.civicRegNr"].StringValue.Optional),
                streetAddress = formatNull(customerCard[$"a{n}.addressStreet"].StringValue.Optional),
                phone = formatNull(customerCard[$"a{n}.phone"].StringValue.Optional),
                email = formatNull(customerCard[$"a{n}.email"].StringValue.Optional),
                areaAndZipcode = formatNull(customerCard[$"a{n}.addressCity"].StringValue.Optional + " " + customerCard[$"a{n}.addressZipcode"].StringValue.Optional)
            };

            Func<int, ApplicationQuestion> getQuestionsByApplicantNr = applicantNr =>
            {
                var a = $"applicant{applicantNr}";
                var baseLanguage = NEnv.ClientCfg.Country.GetBaseLanguage();
                Func<string, string> getTranslatedCreditApplicationItemEnum = (itemName) =>
                {
                    var v = ci[itemName].StringValue.Optional;
                    if (string.IsNullOrWhiteSpace(v))
                        return null;
                    return customFieldsService.GetFieldModel("CreditApplicationItem", itemName)?.Translations?.Opt(baseLanguage)?.Opt(v) ?? v;
                };

                return new ApplicationQuestion
                {
                    employment = formatNull(getTranslatedCreditApplicationItemEnum($"{a}.employment")),
                    childrenMinorCount = formatNull(ci[$"{a}.childrenMinorCount"].IntValue.Optional?.ToString()),
                    childrenAdultCount = formatNull(ci[$"{a}.childrenAdultCount"].IntValue.Optional?.ToString()),
                    isFirstTimeBuyer = formatNull(getTranslatedCreditApplicationItemEnum($"{a}.isFirstTimeBuyer")),
                    employedSince = formatNull(ci[$"{a}.employedSince"].MonthValue(true).Optional?.ToString("yyyy-MM")),
                    employer = formatNull(ci[$"{a}.employer"].StringValue.Optional),
                    profession = formatNull(ci[$"{a}.profession"].StringValue.Optional),
                    employedTo = formatNull(ci[$"{a}.employedTo"].StringValue.Optional),
                    marriage = formatNull(getTranslatedCreditApplicationItemEnum($"{a}.marriage")),
                    monthlyIncomeSalaryAmount = formatNull(ci[$"{a}.monthlyIncomeSalaryAmount"].DecimalValue.Optional?.ToString("C", F)),
                    monthlyIncomePensionAmount = formatNull(ci[$"{a}.monthlyIncomePensionAmount"].DecimalValue.Optional?.ToString("C", F)),
                    monthlyIncomeCapitalAmount = formatNull(ci[$"{a}.monthlyIncomeCapitalAmount"].DecimalValue.Optional?.ToString("C", F)),
                    monthlyIncomeBenefitsAmount = formatNull(ci[$"{a}.monthlyIncomeBenefitsAmount"].DecimalValue.Optional?.ToString("C", F)),
                    monthlyIncomeOtherAmount = formatNull(ci[$"{a}.monthlyIncomeOtherAmount"].DecimalValue.Optional?.ToString("C", F)),
                    costOfLivingRent = formatNull(ci[$"{a}.costOfLivingRent"].DecimalValue.Optional?.ToString("C", F)),
                    costOfLivingFees = formatNull(ci[$"{a}.costOfLivingFees"].DecimalValue.Optional?.ToString("C", F)),
                    answeredYesOnPepQuestion = formatNull(questions?.Items?.Where(x => x.QuestionCode == "isPep" && x.ApplicantNr == applicantNr)?.Select(x => x.AnswerText)?.FirstOrDefault()),
                    pepRoles = formatNull(questions?.Items?.Where(x => x.QuestionCode == "pepRole" && x.ApplicantNr == applicantNr)?.Select(x => x.AnswerText)?.FirstOrDefault()),
                    pepWho = formatNull(questions?.Items?.Where(x => x.QuestionCode == "pepWho" && x.ApplicantNr == applicantNr)?.Select(x => x.AnswerText)?.FirstOrDefault()),
                    taxCountries = formatNull(questions?.Items?.Where(x => x.QuestionCode == "taxCountries" && x.ApplicantNr == applicantNr)?.Select(x => x.AnswerText)?.FirstOrDefault())
                };
            };

            var poaItems = CreatePowerOfAttorney(applicantNo, getContactInfoByApplicantNr, data);

            context = new MortgageLoanDualApplicationAndPoaPrintContext
            {
                application = onlyPoaForBankName != null ? null : new ApplicationModel
                {
                    contact = getContactInfoByApplicantNr(applicantNo),
                    contact1 = getContactInfoByApplicantNr(1),
                    questions1 = getQuestionsByApplicantNr(1),
                    contact2 = nrOfApplicants > 1 ? getContactInfoByApplicantNr(2) : null,
                    questions2 = nrOfApplicants > 1 ? getQuestionsByApplicantNr(2) : null,
                },
                printDate = DateTime.Now.ToString("d", F),
                poa = onlyApplication
                    ? null :
                    (onlyPoaForBankName == null ? poaItems : poaItems.Where(x => x.bankName == onlyPoaForBankName).ToList())
            };

            failedMessage = null;
            return true;
        }

        public Dictionary<int, List<string>> GetPoaBankNames(string applicationNr)
        {
            var requestedData = new Dictionary<string, HashSet<string>>
            {
                { "ComplexApplicationList", new HashSet<string>() }
            };

            EnsureLoansRequested(requestedData);

            var data = applicationDataSourceService.GetDataSimple(applicationNr, requestedData);

            return GetBankItems(data)
                .SelectMany(x => x.ApplicantNrs.Select(y => new { ApplicantNr = y, x.BankName }))
                .GroupBy(x => x.ApplicantNr)
                .ToDictionary(x => x.Key, x => x.Select(y => y.BankName).ToList());
        }

        private class PoaBankItem
        {
            public string BankName { get; internal set; }
            public List<int> ApplicantNrs { get; internal set; }
            public int OrderNr { get; internal set; }
        }

        private List<PoaBankItem> GetBankItems(ApplicationDataSourceResult data)
        {
            var rows = ComplexApplicationListDataSource.ToRows(data.DataSource("ComplexApplicationList"));

            return rows
                .Where(x => x.ListName.IsOneOf("CurrentMortgageLoans", "CurrentOtherLoans"))
                .OrderBy(x => x.Nr)
                .Select(x => new
                {
                    Nr = x.Nr,
                    LoanApplicant1IsParty = x.UniqueItems.Opt("loanApplicant1IsParty") == "true",
                    LoanApplicant2IsParty = x.UniqueItems.Opt("loanApplicant2IsParty") == "true",
                    LoanShouldBeSettled = x.UniqueItems.Opt("loanShouldBeSettled") == "true",
                    BankName = x.UniqueItems.Opt("bankName")
                })
                .Where(x => x.LoanShouldBeSettled && (x.LoanApplicant1IsParty || x.LoanApplicant2IsParty))
                .SelectMany(x => Enumerables.SkipNulls(
                    x.LoanApplicant1IsParty ? new { x.Nr, x.BankName, ApplicantNr = 1 } : null,
                    x.LoanApplicant2IsParty ? new { x.Nr, x.BankName, ApplicantNr = 2 } : null))
                .GroupBy(x => x.BankName)
                .Select(x => new PoaBankItem
                {
                    BankName = x.Key,
                    ApplicantNrs = x.Select(y => y.ApplicantNr).Distinct().OrderBy(y => y).ToList(),
                    OrderNr = x.Min(y => y.Nr)
                })
                .OrderBy(x => x.OrderNr)
                .ToList();
        }

        public List<PaoModel> CreatePowerOfAttorney(int applicantNr, Func<int, ApplicationContactInfo> getContactInfoByApplicantNr, ApplicationDataSourceResult data)
        {
            var rows = ComplexApplicationListDataSource.ToRows(data.DataSource("ComplexApplicationList"));

            var banks = GetBankItems(data);

            var poa = new List<PaoModel>();
            foreach (var bank in banks.Where(x => x.ApplicantNrs.Contains(applicantNr))) //No need to show a poa to an applicant who has no part in it
            {
                var p = new PaoModel
                {
                    bankName = bank.BankName,
                    poacontact1 = bank.ApplicantNrs.Count > 0 ? getContactInfoByApplicantNr(bank.ApplicantNrs[0]) : null,
                    poacontact2 = bank.ApplicantNrs.Count > 1 ? getContactInfoByApplicantNr(bank.ApplicantNrs[1]) : null
                };
                poa.Add(p);
            }

            return poa;
        }

        protected CultureInfo F => this.formattingCulture.Value;

        public MemoryStream CreateApplicationAndPoaDocument(MortgageLoanDualApplicationAndPoaPrintContext context, string overrideTemplateName = null, bool? disableTemplateCache = false)
        {
            var dc = new nDocumentClient();

            var pdfBytes = dc.PdfRenderDirect(
                overrideTemplateName ?? "mortgageloan-application-and-poa",
                PdfCreator.ToTemplateContext(context),
                disableTemplateCache: disableTemplateCache.GetValueOrDefault());

            return new MemoryStream(pdfBytes);
        }
    }

    public interface IMortgageLoanDualApplicationAndPoaService
    {
        bool TryGetPrintContext(string applicationNr, int applicantNo, bool onlyApplication, string onlyPoaForBankName, out MortgageLoanDualApplicationAndPoaPrintContext context, out string failedMessage);

        Dictionary<int, List<string>> GetPoaBankNames(string applicationNr);

        MemoryStream CreateApplicationAndPoaDocument(MortgageLoanDualApplicationAndPoaPrintContext context, string overrideTemplateName = null, bool? disableTemplateCache = false);
    }

    public class MortgageLoanDualApplicationAndPoaPrintContext
    {
        public string printDate { get; set; }
        public ApplicationModel application { get; set; }
        public List<PaoModel> poa { get; set; }

        public class PaoModel
        {
            public ApplicationContactInfo poacontact1 { get; set; }
            public ApplicationContactInfo poacontact2 { get; set; }
            public string bankName { get; set; }
        }

        public class ApplicationModel
        {
            public ApplicationContactInfo contact { get; set; }
            public ApplicationContactInfo contact1 { get; set; }
            public ApplicationContactInfo contact2 { get; set; }
            public ApplicationQuestion questions1 { get; set; }
            public ApplicationQuestion questions2 { get; set; }
        }

        public class ApplicationContactInfo
        {
            public string civicRegNr { get; set; }
            public string fullName { get; set; }
            public string streetAddress { get; set; }
            public string areaAndZipcode { get; set; }
            public string phone { get; set; }
            public string email { get; set; }
        }

        public class ApplicationQuestion
        {
            public string isFirstTimeBuyer { get; set; }
            public string employment { get; set; }
            public string employedSince { get; set; }
            public string employer { get; set; }
            public string profession { get; set; }
            public string employedTo { get; set; }
            public string marriage { get; set; }
            public string monthlyIncomeSalaryAmount { get; set; }
            public string monthlyIncomePensionAmount { get; set; }
            public string monthlyIncomeCapitalAmount { get; set; }
            public string monthlyIncomeBenefitsAmount { get; set; }
            public string monthlyIncomeOtherAmount { get; set; }
            public string childrenMinorCount { get; set; }
            public string childrenAdultCount { get; set; }
            public string costOfLivingRent { get; set; }
            public string costOfLivingFees { get; set; }
            public string answeredYesOnPepQuestion { get; set; }
            public string pepRoles { get; set; }
            public string pepWho { get; set; }
            public string taxCountries { get; set; }
        }
    }
}