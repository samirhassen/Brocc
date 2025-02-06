using nCredit.DbModel.BusinessEvents.NewCredit;
using nCredit.DomainModel;
using NTech;
using NTech.Banking.CivicRegNumbers;
using NTech.Banking.Conversion;
using NTech.Banking.LoanModel;
using NTech.Banking.OrganisationNumbers;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.DbModel.BusinessEvents
{
    public class CompanyLoanExcelFileImportOperation : BusinessEventManagerOrServiceBase
    {
        private List<PersonModel> Persons { get; set; } = new List<PersonModel>();
        private List<LoanModel> Loans { get; set; } = new List<LoanModel>();
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        private List<Tuple<string, string>> Summaries { get; set; } = new List<Tuple<string, string>>();
        private readonly Lazy<CivicRegNumberParser> civicRegNrParser;
        private readonly Lazy<OrganisationNumberParser> orgNrParser;
        private readonly CreditContextFactory contextFactory;
        private readonly ICustomerClient customerClient;
        private readonly ICreditEnvSettings creditEnvSettings;

        private Dictionary<string, int> customerIdByOrgNr { get; set; }
        private Dictionary<string, int> customerIdByCivicRegNr { get; set; }
        private Lazy<string> excelSourceArchiveKey;

        private CompanyLoanExcelFileImportOperation(ICoreClock clock, IClientConfigurationCore clientConfiguration, INTechCurrentUserMetadata user, 
            CreditContextFactory contextFactory, ICustomerClient customerClient, ICreditEnvSettings creditEnvSettings) : base(user, clock, clientConfiguration)
        {
            this.contextFactory = contextFactory;
            this.customerClient = customerClient;
            this.creditEnvSettings = creditEnvSettings;
            civicRegNrParser = new Lazy<CivicRegNumberParser>(() => new CivicRegNumberParser(clientConfiguration.Country.BaseCountry));
            orgNrParser = new Lazy<OrganisationNumberParser>(() => new OrganisationNumberParser(clientConfiguration.Country.BaseCountry));
        }

        public static CompanyLoanExcelFileImportOperation BeginWithDataUrl(string fileName, string excelFileAsDataUrl, 
            INTechCurrentUserMetadata user, ICoreClock clock, IClientConfigurationCore clientConfiguration, IDocumentClient documentClient,
            CreditContextFactory contextFactory, ICustomerClient customerClient, ICreditEnvSettings envSettings)
        {
            var p = new CompanyLoanExcelFileImportOperation(clock, clientConfiguration, user, contextFactory, customerClient, envSettings);

            var dc = documentClient;

            if (!Files.TryParseDataUrl(excelFileAsDataUrl, out var ct, out var b))
            {
                p.Error("Invalid file");
                return p;
            }

            p.excelSourceArchiveKey = new Lazy<string>(() =>
            {
                return dc.ArchiveStore(b, ct, fileName);
            });

            Dictionary<string, List<List<string>>> excelTable;
            try
            {
                excelTable = dc.ParseDataUrlExcelFile(fileName, excelFileAsDataUrl, false);
            }
            catch(Exception ex)
            {
                p.Error("Parsing failed: " + ex.Message);
                return p;
            }

            p.DoParse(excelTable);
            return p;
        }

        private void Error(string msg)
        {
            Errors.Add(msg);
        }

        private void Warning(string msg)
        {
            Warnings.Add(msg);
        }

        private void Summary(string label, string text)
        {
            Summaries.Add(Tuple.Create(label, text));
        }

        private void Summary(string label, decimal nr)
        {
            Summaries.Add(Tuple.Create(label, nr.ToString("#,0.####", CommentFormattingCulture)));
        }

        public List<Tuple<string, string>> ImportLoans(NewCreditBusinessEventManager mgr)
        {
            if (Errors.Any())
                throw new Exception("Import not allowed when errors exist!");

            var cc = customerClient;
            Func<string, string, CreateOrUpdatePersonRequest.Property> pp = (n, v) => new CreateOrUpdatePersonRequest.Property
            {
                ForceUpdate = true,
                Name = n,
                Value = v
            };
            Func<string, string, CreateOrUpdateCompanyRequest.Property> pc = (n, v) => new CreateOrUpdateCompanyRequest.Property
            {
                ForceUpdate = true,
                Name = n,
                Value = v
            };

            var selfProviderName = creditEnvSettings.GetAffiliateModels().Where(x => x.IsSelf).First().ProviderName;
            var creditNrGenerator = new CreditNrGenerator(contextFactory);
            var creditNrsResult = new List<Tuple<string, string>>();

            foreach (var loan in Loans)
            {
                var loanRequest = new NewCreditRequest
                {
                    AnnuityAmount = loan.AnnuityAmount,
                    BeforeImportCreditNr = loan.CreditNr,
                    CreditNr = creditNrGenerator.GenerateNewCreditNr(),
                    SniKodSe = loan.CompanySniCodeSe,
                    CompanyLoanApplicantCustomerIds = new List<int>(),
                    CompanyLoanAuthorizedSignatoryCustomerIds = new List<int>(),
                    CompanyLoanBeneficialOwnerCustomerIds = new List<int>(),
                    CompanyLoanCollateralCustomerIds = new List<int>(),
                    CreditAmount = loan.CreditAmount,
                    IsCompanyCredit = true,
                    MarginInterestRatePercent = loan.MarginInterestRatePercent,
                    NotificationFee = loan.NotificationFee,
                    ProviderName = selfProviderName,
                    ApplicationFreeformDocumentArchiveKeys = new List<string>(),
                    IsInitialPaymentAlreadyMade = true,
                    ApplicationProbabilityOfDefault = loan.PD,
                    ApplicationLossGivenDefault = loan.LGD
                };
                var persons = Persons.Where(x => x.CreditNr == loan.CreditNr).ToList();
                foreach (var p in persons)
                {
                    var customerId = customerIdByCivicRegNr[p.CivicRegNr.NormalizedValue];
                    var properties = new List<CreateOrUpdatePersonRequest.Property>
                            {
                                pp("firstName", p.FirstName),
                                pp("lastName", p.LastName),
                                pp("email", p.Email),
                                pp("phone", p.PhoneNr)
                            };
                    if (!string.IsNullOrWhiteSpace(p.AddressStreet))
                        properties.Add(pp("addressStreet", p.AddressStreet));

                    if (!string.IsNullOrWhiteSpace(p.AddressZipCode))
                        properties.Add(pp("addressZipcode", p.AddressZipCode));

                    if (!string.IsNullOrWhiteSpace(p.AddressCity))
                        properties.Add(pp("addressCity", p.AddressCity));

                    cc.CreateOrUpdatePerson(new CreateOrUpdatePersonRequest
                    {
                        ExpectedCustomerId = customerId,
                        CivicRegNr = p.CivicRegNr.NormalizedValue,
                        Properties = properties,
                        EventSourceId = loanRequest.CreditNr,
                        EventType = "ImportCompanyLoan"
                    });

                    if (p.IsApplicant)
                        loanRequest.CompanyLoanApplicantCustomerIds.Add(customerId);
                    if (p.IsAuthorizedSignatory)
                        loanRequest.CompanyLoanAuthorizedSignatoryCustomerIds.Add(customerId);
                    if (p.IsBeneficialOwner)
                        loanRequest.CompanyLoanBeneficialOwnerCustomerIds.Add(customerId);
                    if (p.IsCollateral)
                        loanRequest.CompanyLoanCollateralCustomerIds.Add(customerId);
                }

                var companyCustomerId = customerIdByOrgNr[loan.CompanyOrgNr.NormalizedValue];

                var cp = new List<CreateOrUpdateCompanyRequest.Property>
                        {
                            pc("email", loan.CompanyEmail),
                            pc("phone", loan.CompanyPhoneNr),
                            pc("addressStreet", loan.CompanyAddressStreet),
                            pc("addressZipcode", loan.CompanyAddressZipCode),
                            pc("addressCity", loan.CompanyAddressCity)
                        };
                if (!string.IsNullOrWhiteSpace(loan.CompanySniCodeSe))
                    cp.Add(pc("snikod", loan.CompanySniCodeSe));

                cc.CreateOrUpdateCompany(new CreateOrUpdateCompanyRequest
                {
                    CompanyName = loan.CompanyName,
                    ExpectedCustomerId = companyCustomerId,
                    Orgnr = loan.CompanyOrgNr.NormalizedValue,
                    Properties = cp,
                    EventSourceId = loanRequest.CreditNr,
                    EventType = "ImportCompanyLoan"
                });

                loanRequest.Applicants = new List<NewCreditRequest.Applicant>
                    {
                        new NewCreditRequest.Applicant
                        {
                            ApplicantNr = 1,
                            CustomerId = companyCustomerId
                        }
                    };
                loanRequest.NrOfApplicants = 1;
                loanRequest.ApplicationFreeformDocumentArchiveKeys.Add(this.excelSourceArchiveKey.Value);

                using (var context = contextFactory.CreateContext())
                {
                    context.DoUsingTransaction(() =>
                    {
                        var model = new SharedDatedValueDomainModel(context);
                        var getReferenceInterest = new Lazy<decimal>(() => model.GetReferenceInterestRatePercent(Clock.Today));
                        var credit = mgr.CreateNewCredit(context, loanRequest, getReferenceInterest);
                        context.SaveChanges();
                    });
                }
                creditNrsResult.Add(Tuple.Create(loan.CreditNr, loanRequest.CreditNr));
            }

            return creditNrsResult;
        }

        public Tuple<List<Tuple<string, string>>, List<LoanModel>, List<PersonModel>> CheckConsistenyAndGeneratePreview()
        {
            Action<string, IEnumerable<string>> errWithStringList = (msg, sl) =>
            {
                if (sl.Any())
                    Error($"{msg} ({string.Join(", ", sl)})");
            };
            Action<string, IEnumerable<string>> warnWithStringList = (msg, sl) =>
            {
                if (sl.Any())
                    Warning($"{msg} ({string.Join(", ", sl)})");
            };

            errWithStringList("Loans: Duplicate CreditNrs", Loans
                .GroupBy(x => x.CreditNr)
                .Select(x => new
                {
                    x.Key,
                    Count = x.Count()
                })
                .Where(x => x.Count > 1)
                .Select(x => x.Key)
                .Distinct()
                .ToList());

            //Credit/Person matching
            var personsCreditNrs = Persons.Select(x => x.CreditNr).ToHashSetShared();
            var loansCreditNrs = Loans.Select(x => x.CreditNr).ToHashSetShared();

            errWithStringList("Persons: CreditNrs that have no matching Loan", personsCreditNrs.Except(loansCreditNrs));
            errWithStringList("Loans: CreditNrs that have no matching Person", loansCreditNrs.Except(personsCreditNrs));
            errWithStringList("Loans: CreditNrs that have no matching ApplicantCustomer", loansCreditNrs.Except(Persons.Where(x => x.IsApplicant).Select(x => x.CreditNr).ToHashSetShared()));

            Persons.Select(x => new
            {
                person = x,
                hash = Hashes.Md5(x.CivicRegNr.NormalizedValue, x.FirstName, x.LastName, x.PhoneNr, x.Email)
            })
            .GroupBy(x => x.person.CivicRegNr.NormalizedValue)
            .Where(x => x.Select(y => y.hash).Distinct().Count() > 1)
            .ToList()
            .ForEach(x =>
            {
                Warning($"Person {x.Key} exists on several loans with different contact info. CreditNrs: {string.Join(", ", x.Select(y => y.person.CreditNr).Distinct())}");
            });

            Loans.Select(x => new
            {
                loan = x,
                hash = Hashes.Md5(x.CompanyOrgNr.NormalizedValue, x.CompanyName, x.CompanyEmail, x.CompanyPhoneNr, x.CompanySniCodeSe)
            })
            .GroupBy(x => x.loan.CompanyOrgNr.NormalizedValue)
            .Where(x => x.Select(y => y.hash).Distinct().Count() > 1)
            .ToList()
            .ForEach(x =>
            {
                Warning($"Company {x.Key} exists on several loans with different contact info. CreditNrs: {string.Join(", ", x.Select(y => y.loan.CreditNr).Distinct())}");
            });

            //Excel stores 12.5% as 0.125 so we need to warn the user if this sneaks through our earlier parser defence
            var smallInterestLoans = Loans
                .Where(x => x.MarginInterestRatePercent > 0m && x.MarginInterestRatePercent < 1m)
                .Select(x => x.CreditNr);
            if (smallInterestLoans.Any())
                Warning($"These loans have an interest rate between 0% and 1%. Check to make sure it's not an excel translation error: {string.Join(", ", smallInterestLoans)}");

            //Persons with no connections at all
            Persons
                .Where(x => !x.IsApplicant && !x.IsAuthorizedSignatory && !x.IsBeneficialOwner && !x.IsCollateral)
                .Select((x, i) => i + 1)
                .ToList()
                .ForEach(x => Warning($"Person {x} has no role on any loan"));

            //Check for existing loans on the same companies
            PopulateCustomerIds();

            var remPayCalculator = new RemainingPaymentsCalculation();

            var allCompanyCustomerIds = customerIdByOrgNr.Values;
            var allCreditNrs = Loans.Select(x => x.CreditNr).Distinct().ToList();
            using (var context = contextFactory.CreateContext())
            {
                var currentInterestRate = new SharedDatedValueDomainModel(context).GetReferenceInterestRatePercent(Clock.Today);
                Loans.ForEach(loan =>
                {
                    try
                    {
                        var totalInterestRate = loan.MarginInterestRatePercent + currentInterestRate;
                        remPayCalculator.ComputeWithAnnuity(null, DateTime.Now, loan.CreditAmount,
                            totalInterestRate, loan.AnnuityAmount);
                    }
                    catch (OverflowException) // Could not calculate. 
                    {
                        Error($"Loan {loan.CreditNr} will never be repaid. ");
                    }
                });

                var existingCredits = context
                    .CreditHeadersQueryable
                    .Where(x => x.CreditType == CreditType.CompanyLoan.ToString())
                    .Select(x => new
                    {
                        CreationTransactionDate = x.CreatedByEvent.TransactionDate,
                        x.CreditNr,
                        CompanyCustomerId = x.CreditCustomers.Select(y => y.CustomerId).FirstOrDefault(),
                        BeforeImportCreditNr = x.DatedCreditStrings.Where(y => y.Name == "BeforeImportCreditNr").Select(y => y.Value).FirstOrDefault()
                    })
                    .Where(x => allCompanyCustomerIds.Contains(x.CompanyCustomerId) || (x.BeforeImportCreditNr != null && allCreditNrs.Contains(x.BeforeImportCreditNr)))
                    .ToList();

                foreach (var e in existingCredits)
                {
                    if (allCreditNrs.Contains(e.BeforeImportCreditNr))
                        Error($"Loan {e.BeforeImportCreditNr} has already been imported");
                    else if (Dates.GetAbsoluteNrOfDaysBetweenDates(Clock.Today, e.CreationTransactionDate) < 180)
                    {
                        var orgnr = customerIdByOrgNr.Single(x => x.Value == e.CompanyCustomerId).Key;
                        Warning($"Company {orgnr} has a recently created loan {e.CreditNr}");
                    }
                }
            }

            if (Errors.Any())
                return null;

            Summary("Nr of loans", Loans.Count.ToString("#,0", CommentFormattingCulture));
            Summary("Total CreditAmount", Loans.Sum(x => x.CreditAmount));
            Summary("Min CreditAmount", Loans.Min(y => y.CreditAmount));
            Summary("Max CreditAmount", Loans.Max(y => y.CreditAmount));
            Summary("Min MarginInterestRatePercent", Loans.Min(y => y.MarginInterestRatePercent));
            Summary("Max MarginInterestRatePercent", Loans.Max(y => y.MarginInterestRatePercent));
            Summary("Min Annuity", Loans.Min(y => y.AnnuityAmount));
            Summary("Max Annuity", Loans.Max(y => y.AnnuityAmount));
            Summary("Min NotificationFee", Loans.Min(y => y.NotificationFee));
            Summary("Max NotificationFee", Loans.Max(y => y.NotificationFee));

            return Tuple.Create(Summaries, Loans, Persons);
        }

        private void PopulateCustomerIds()
        {
            customerIdByCivicRegNr = new Dictionary<string, int>();
            customerIdByOrgNr = new Dictionary<string, int>();

            var allCivNrs = Persons.Select(x => x.CivicRegNr.NormalizedValue).Distinct().ToList();
            var allAllOrgNrs = Loans.Select(x => x.CompanyOrgNr.NormalizedValue).Distinct().ToList();
            var cc = customerClient;
            var customerIds = cc.FetchCustomersIds(
                allCivNrs,
                allAllOrgNrs);

            for (var i = 0; i < allCivNrs.Count; i++)
            {
                customerIdByCivicRegNr[allCivNrs[i]] = customerIds.Item1[i];
            }
            for (var j = 0; j < allAllOrgNrs.Count; j++)
            {
                customerIdByOrgNr[allAllOrgNrs[j]] = customerIds.Item2[j];
            }
        }

        private void DoParse(Dictionary<string, List<List<string>>> d)
        {
            if (d == null)
            {
                Error("No data found");
                return;
            }
            ParsePersons(d.Opt("Persons"));
            ParseLoans(d.Opt("Loans"));
            if (!Errors.Any() && !Loans.Any())
                Error("No loans");
        }

        private void ParsePersons(List<List<string>> d)
        {
            if (d == null)
            {
                Error("Persons missing");
                return;
            }
            if (d.Count == 0)
            {
                Error("Persons is empty");
                return;
            }
            try
            {
                var columnIndexByName = d[0].Select((x, i) => new { x, i }).ToDictionary(y => y.x, y => y.i);
                for (var i = 1; i < d.Count; i++)
                {
                    var p = ParsePerson(columnIndexByName, d[i], i);
                    if (p != null)
                        Persons.Add(p);
                }
            }
            catch (ParsingException ex)
            {
                Error($"Persons: {ex.Message}");
            }
        }

        private PersonModel ParsePerson(Dictionary<string, int> indexByName, List<string> row, int rowIndex)
        {
            try
            {
                var isCollateral = ParseCellValue("IsCollateral", indexByName, row, false, ParseBoolean);

                return new PersonModel
                {
                    CreditNr = ParseCellValue("CreditNr", indexByName, row, true, x => x),
                    CivicRegNr = ParseCellValue("CivicRegNr", indexByName, row, true, ParseCivicRegNr),
                    FirstName = ParseCellValue("FirstName", indexByName, row, true, x => x),
                    LastName = ParseCellValue("LastName", indexByName, row, true, x => x),
                    Email = ParseCellValue("Email", indexByName, row, true, ParseEmail),
                    PhoneNr = ParseCellValue("PhoneNr", indexByName, row, true, ParsePhoneNr),
                    IsApplicant = ParseCellValue("IsApplicant", indexByName, row, false, ParseBoolean),
                    IsAuthorizedSignatory = ParseCellValue("IsAuthorizedSignatory", indexByName, row, false, ParseBoolean),
                    IsBeneficialOwner = ParseCellValue("IsBeneficialOwner", indexByName, row, false, ParseBoolean),
                    IsCollateral = isCollateral,
                    AddressStreet = ParseCellValue("AddressStreet", indexByName, row, isCollateral, x => x),
                    AddressZipCode = ParseCellValue("AddressZipCode", indexByName, row, isCollateral, x => x),
                    AddressCity = ParseCellValue("AddressCity", indexByName, row, isCollateral, x => x)
                };
            }
            catch (ParsingException ex)
            {
                Error($"Person {rowIndex + 1}: {ex.Message}");
                return null;
            }
        }

        private LoanModel ParseLoan(Dictionary<string, int> indexByName, List<string> row, int rowIndex)
        {
            try
            {
                return new LoanModel
                {
                    CreditNr = ParseCellValue("CreditNr", indexByName, row, true, x => x),
                    AnnuityAmount = ParseCellValue("AnnuityAmount", indexByName, row, true, ParseDecimal),
                    CreditAmount = ParseCellValue("CreditAmount", indexByName, row, true, ParseDecimal),
                    MarginInterestRatePercent = ParseCellValue("MarginInterestRatePercent", indexByName, row, true, ParseDecimal),
                    CompanyEmail = ParseCellValue("CompanyEmail", indexByName, row, true, ParseEmail),
                    CompanyPhoneNr = ParseCellValue("CompanyPhoneNr", indexByName, row, true, ParsePhoneNr),
                    CompanyName = ParseCellValue("CompanyName", indexByName, row, true, x => x),
                    CompanyOrgNr = ParseCellValue("CompanyOrgNr", indexByName, row, true, ParseOrgNr),
                    CompanySniCodeSe = ParseCellValue("CompanySniCodeSe", indexByName, row, false, x => x),
                    NotificationFee = ParseCellValue("NotificationFee", indexByName, row, true, ParseDecimal),
                    CompanyAddressStreet = ParseCellValue("CompanyAddressStreet", indexByName, row, true, x => x),
                    CompanyAddressZipCode = ParseCellValue("CompanyAddressZipCode", indexByName, row, true, x => x),
                    CompanyAddressCity = ParseCellValue("CompanyAddressCity", indexByName, row, true, x => x),
                    PD = ParseCellValue("PD", indexByName, row, false, x => string.IsNullOrWhiteSpace(x) ? new decimal?() : ParseDecimal(x)),
                    LGD = ParseCellValue("LGD", indexByName, row, false, x => string.IsNullOrWhiteSpace(x) ? new decimal?() : ParseDecimal(x)),
                };
            }
            catch (ParsingException ex)
            {
                Error($"Loan {rowIndex + 1}: {ex.Message}");
                return null;
            }
        }

        private decimal ParseDecimal(string v)
        {
            var r = Numbers.ParseDecimalOrNull(v);
            if (!r.HasValue)
                throw new ParsingException("Invalid number. Format should be 99 or 99.99");
            if (r.Value < 0m)
                throw new ParsingException("Invalid number. Cannot be negative");
            return r.Value;
        }

        private string ParseEmail(string v)
        {
            if (!v.Contains("@"))
                throw new ParsingException("Invalid email");
            return v;
        }

        private string ParsePhoneNr(string v)
        {
            if (v.Any(Char.IsLetter))
                throw new ParsingException("Invalid phonenr");
            return v;
        }

        private bool ParseBoolean(string v)
        {
            return v.IsOneOfIgnoreCase("true", "x", "yes");
        }

        private ICivicRegNumber ParseCivicRegNr(string v)
        {
            if (civicRegNrParser.Value.TryParse(v, out var c))
                return c;
            else
                throw new ParsingException("Invalid civicRegNr");
        }

        private IOrganisationNumber ParseOrgNr(string v)
        {
            if (orgNrParser.Value.TryParse(v, out var c))
                return c;
            else
                throw new ParsingException("Invalid orgNr");
        }

        private T ParseCellValue<T>(string headerName, Dictionary<string, int> indexByName, List<string> row, bool isRequired, Func<string, T> parse)
        {
            if (!indexByName.ContainsKey(headerName))
                throw new ParsingException($"{headerName} is missing from the header row");
            var i = indexByName[headerName];
            if (!(i < row.Count))
                throw new ParsingException($"The column '{headerName}' is missing");
            var v = row[i];
            if (isRequired && string.IsNullOrWhiteSpace(v))
                throw new ParsingException($"The column '{headerName}' is required and missing a value");

            v = v.Replace("\r", "").Replace("\n", "").Replace("\t", " ").Trim();

            return parse(v);
        }

        private void ParseLoans(List<List<string>> d)
        {
            if (d == null)
            {
                Error("Loans missing");
                return;
            }
            if (d.Count == 0)
            {
                Error("Loans is empty");
                return;
            }
            try
            {
                var columnIndexByName = d[0].Select((x, i) => new { x, i }).ToDictionary(y => y.x, y => y.i);
                for (var i = 1; i < d.Count; i++)
                {
                    var p = ParseLoan(columnIndexByName, d[i], i);
                    if (p != null)
                        Loans.Add(p);
                }
            }
            catch (ParsingException ex)
            {
                Error($"Loans: {ex.Message}");
            }
        }

        public class PersonModel
        {
            public string CreditNr { get; set; }
            public ICivicRegNumber CivicRegNr { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string PhoneNr { get; set; }
            public bool IsApplicant { get; set; }
            public bool IsAuthorizedSignatory { get; set; }
            public bool IsBeneficialOwner { get; set; }
            public bool IsCollateral { get; set; }
            public string AddressStreet { get; set; }
            public string AddressZipCode { get; set; }
            public string AddressCity { get; set; }
        }

        public class LoanModel
        {
            public string CreditNr { get; set; }
            public decimal AnnuityAmount { get; set; }
            public decimal CreditAmount { get; set; }
            public decimal NotificationFee { get; set; }
            public decimal MarginInterestRatePercent { get; set; }
            public string CompanySniCodeSe { get; set; }
            public IOrganisationNumber CompanyOrgNr { get; set; }
            public string CompanyName { get; set; }
            public string CompanyEmail { get; set; }
            public string CompanyPhoneNr { get; set; }
            public string CompanyAddressStreet { get; set; }
            public string CompanyAddressZipCode { get; set; }
            public string CompanyAddressCity { get; set; }
            public decimal? PD { get; set; }
            public decimal? LGD { get; set; }
        }

        public class ParsingException : Exception
        {
            public ParsingException(string msg) : base(msg)
            {
            }
        }
    }
}