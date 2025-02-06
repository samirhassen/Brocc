using Dapper;
using Newtonsoft.Json;
using NTech.Banking.CivicRegNumbers.Se;
using NTech.Banking.OrganisationNumbers;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Models;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace NTech.Core.Credit.Shared.Services.SwedishMortgageLoans
{
    public class SwedishMortgageLoanImportService
    {
        private readonly IDocumentClient documentClient;
        private readonly ICoreClock clock;
        private readonly CreditContextFactory contextFactory;
        private readonly ICustomerClient customerClient;
        private readonly SwedishMortageLoanCreationService creationService;

        public SwedishMortgageLoanImportService(IDocumentClient documentClient, ICoreClock clock, CreditContextFactory contextFactory, ICustomerClient customerClient, SwedishMortageLoanCreationService creationService)
        {
            this.documentClient = documentClient;
            this.clock = clock;
            this.contextFactory = contextFactory;
            this.customerClient = customerClient;
            this.creationService = creationService;
        }

        public SwedishMortgageLoanImportResponse ImportFile(SwedishMortgageLoanImportRequest request)
        {
            if(!request.FileName.EndsWith(".xlsx", StringComparison.InvariantCultureIgnoreCase))
            {
                return new SwedishMortgageLoanImportResponse
                {
                    Errors = new List<string>
                    {
                        "File must be and xlsx file"
                    }
                };
            }

            if(request.AgreementArchiveKey != null && (request.AgreementFileName != null || request.AgreementFileBase64Data != null))
            {
                return new SwedishMortgageLoanImportResponse
                {
                    Errors = new List<string>
                    {
                        "Only one of AgreementArchiveKey and AgreementFileName + AgreementFileBase64Data can be used in the same request."
                    }
                };
            }

            var dataUrl = $"data:application/vnd.openxmlformats-officedocument.spreadsheetml.sheet;base64,{request.Base64EncodedExcelFile}";
            var excelFileSheets = documentClient.ParseDataUrlExcelFile(request.FileName, dataUrl, false);
            try
            {
                void FailNow(string text) => throw new NTechCoreWebserviceException(text) { ErrorCode = "failNow" };

                if (excelFileSheets == null || excelFileSheets.Count != 1)
                    FailNow("The excel file must have exactly one sheet");

                List<List<string>> sheetRows = excelFileSheets.Single().Value;
                if (sheetRows.Count == 0) FailNow("The excel sheet has no rows");

                var titleRow = sheetRows[0];
                if (titleRow.Count < 3 || titleRow[0] != "Group" || titleRow[1] != "Name" || titleRow[2] != "Value")
                    FailNow("The first row must be columns titles where the first three columns have to be Group, Name, Value");

                var errors = new List<string>();

                var dataPoints = ParseDataPoints(sheetRows, x => errors.Add(x));
                if (errors.Count > 0)
                    return new SwedishMortgageLoanImportResponse
                    {
                        Errors = errors,
                        Warnings = new List<string>()
                    };

                var customerRequests = GetCustomers(dataPoints);
                if (errors.Count > 0)
                    return new SwedishMortgageLoanImportResponse
                    {
                        Errors = errors,
                        Warnings = new List<string>()
                    };
                var customerIdByApplicantNr = GetCustomerIds(customerRequests);
                var loanRequest = CreateLoanRequest(dataPoints, customerIdByApplicantNr);

                if (errors.Count > 0)
                    return new SwedishMortgageLoanImportResponse
                    {
                        Errors = errors,
                        Warnings = new List<string>()
                    };
                var warnings = new List<string>();
                void AddWarning(string w) => warnings.Add(w);

                CheckCreditNrs(loanRequest, errors.Add, AddWarning);
                CheckDatesAndInterest(loanRequest, errors.Add, AddWarning);

                SwedishAmorteringsunderlag amorteringsunderlag = null;
                if(errors.Count == 0)
                {                    
                    amorteringsunderlag = SwedishMortgageLoanAmortizationBasisService.GetSwedishAmorteringsunderlag(loanRequest.AmortizationBasis);
                }

                string agreementArchiveKey = null;
                if(errors.Count == 0)
                {
                    agreementArchiveKey = HandleAgreementFileReturningArchiveKey(request, loanRequest);
                }

                if(errors.Count == 0 && !request.IsPreviewOnly.GetValueOrDefault())
                {
                    foreach(var customerRequest in customerRequests.Values)
                    {
                        customerClient.CreateOrUpdatePerson(customerRequest);
                    }
                    var loanResponse = creationService.CreateLoans(loanRequest);
                    return new SwedishMortgageLoanImportResponse
                    {
                        LoansCreated = loanResponse
                    };
                }
                else
                {
                    return new SwedishMortgageLoanImportResponse
                    {
                        RawDataPoints = dataPoints.Raw,
                        CreatePreview = loanRequest,
                        Errors = errors,
                        Warnings = warnings,
                        CustomersPreview = customerRequests.Keys.OrderBy(x => x).Select(x => customerRequests[x]).ToList(),
                        AmorteringsunderlagPreview = amorteringsunderlag,
                        IsCreateAllowed = errors.Count == 0,
                        PreviewAgreementArchiveKey = agreementArchiveKey
                    };
                }
            }
            catch(NTechCoreWebserviceException ex)
            {
                if (ex.ErrorCode == "failNow")
                {
                    return new SwedishMortgageLoanImportResponse
                    {
                        Errors = new List<string> { ex.Message },
                        Warnings = new List<string>()
                    };
                }
                else
                    throw;
            }
        }

        private string HandleAgreementFileReturningArchiveKey(SwedishMortgageLoanImportRequest importRequest, SwedishMortgageLoanCreationRequest loanRequest)
        {
            string agreementFileArchiveKey = importRequest.AgreementArchiveKey;
            var creditNr = loanRequest.Loans.First().CreditNr;
            if(agreementFileArchiveKey == null && importRequest.AgreementFileBase64Data != null)
            {
                var data = Convert.FromBase64String(importRequest.AgreementFileBase64Data);
                agreementFileArchiveKey =  documentClient.ArchiveStore(data, "application/pdf", importRequest.AgreementFileName ?? "Agreement.pdf");
            }
            if(agreementFileArchiveKey != null)
            {
                foreach (var applicant in loanRequest.Loans.SelectMany(x => x.Applicants))
                {
                    applicant.AgreementPdfArchiveKey = agreementFileArchiveKey;
                }
            }

            return agreementFileArchiveKey;
        }

        private Dictionary<int, int> GetCustomerIds(Dictionary<int, CreateOrUpdatePersonRequest> customerRequests) =>
            customerRequests.Keys.ToDictionary(
                x => x, 
                x => customerClient.GetCustomerId(CivicRegNumberSe.Parse(customerRequests[x].CivicRegNr)));

        private void CheckDatesAndInterest(SwedishMortgageLoanCreationRequest request, Action<string> addError, Action<string> addWarning)
        {
            var today = clock.Today;

            Dictionary<int, decimal> existingRateByMonthCount;
            using (var context = contextFactory.CreateContext())
            {
                existingRateByMonthCount = context.FixedMortgageLoanInterestRatesQueryable.Select(x => new { x.RatePercent, x.MonthCount }).ToDictionary(x => x.MonthCount, x => x.RatePercent);
            }
            
            foreach(var requestLoan in request.Loans)
            {
                if (requestLoan.AmortizationExceptionUntilDate.HasValue && requestLoan.AmortizationExceptionUntilDate.Value < today)
                    addWarning($"Credit {requestLoan.CreditNr} has AmortizationExceptionUntilDate < today");
                if (requestLoan.ActiveDirectDebitAccount?.ActiveSinceDate != null && requestLoan.ActiveDirectDebitAccount?.ActiveSinceDate > today.AddDays(3))
                    addWarning($"Credit {requestLoan.CreditNr} has direct debit active since day in the future");
                if (!requestLoan.EndDate.HasValue)
                    addError($"Credit {requestLoan.CreditNr} is missing EndDate");
                else
                {
                    var endDate = requestLoan.EndDate.Value;
                    if (endDate < today.AddYears(30))
                        addWarning("EndDate < 30 years in the future. Normally this is 40 years for SE mortgage loans.");
                }
                if (!requestLoan.InterestRebindMounthCount.HasValue)
                    addError($"Credit {requestLoan.CreditNr} missing InterestRebindMounthCount");
                else if(!existingRateByMonthCount.ContainsKey(requestLoan.InterestRebindMounthCount.Value))
                    addWarning($"Credit {requestLoan.CreditNr} has an InterestRebindMounthCount that is not one of the registered ones");
                else
                {
                    var systemRate = existingRateByMonthCount[requestLoan.InterestRebindMounthCount.Value];
                    if (requestLoan.ReferenceInterestRate != systemRate)
                        addWarning($"Credit {requestLoan.CreditNr} has ReferenceInterestRate different from the systems registered one for that rebinding time");                    
                    if(!requestLoan.NextInterestRebindDate.HasValue)
                        addError($"Credit {requestLoan.CreditNr} is missing NextInterestRebindDate");
                    else
                    {
                        var monthCount = requestLoan.InterestRebindMounthCount.Value;
                        var actualMonthCount = Dates.GetAbsoluteNrOfMonthsBetweenDates(requestLoan.NextInterestRebindDate.Value, today);
                        if (Math.Abs(monthCount - actualMonthCount) > 1)
                            addWarning($"Credit {requestLoan.CreditNr} has NextInterestRebindDate which is more than one month off from the given InterestRebindMounthCount");
                    }
                }
            }
            if (request.AmortizationBasis.ObjectValueDate > today)
                addWarning("ObjectValueDate > today");
        }

        private void CheckCreditNrs(SwedishMortgageLoanCreationRequest request, Action<string> addError, Action<string> addWarning)
        {
            long? GetSequenceNr(string creditNr)
            {
                if (!creditNr.StartsWith("L"))
                    return null;
                var nrs = creditNr.Where(Char.IsDigit).ToArray();
                if (nrs.Length != creditNr.Length - 1)
                    return null;
                return long.Parse(new string(nrs));
            }

            using (var context = contextFactory.CreateContext())
            {
                var creditNrs = request.Loans.Select(x => x.CreditNr).ToList();
                var alreadyExistingCreditNrs = context.CreditHeadersQueryable.Where(x => creditNrs.Contains(x.CreditNr)).Select(x => x.CreditNr).ToList();
                alreadyExistingCreditNrs.ForEach(x => addError($"Credit {x} already exists"));
                var nonExistingCreditNrs = creditNrs.Except(alreadyExistingCreditNrs).ToList();
                if(nonExistingCreditNrs.Count > 0)
                {
                    List<long> sequenceNrs = new List<long>();
                    foreach(var creditNr in nonExistingCreditNrs)
                    {
                        var sequenceNr = GetSequenceNr(creditNr);
                        if (sequenceNr == null)
                            addWarning($"Credit {creditNr} uses a non standard nr series");
                        else
                            sequenceNrs.Add(sequenceNr.Value);
                    }
                    var existingSequenceNrs = context.GetConnection().Query<long>("select k.Id from CreditKeySequence k where k.Id in @sequenceNrs", param: new { sequenceNrs }).ToList();
                    foreach(var missingSequenceNr in sequenceNrs.Except(existingSequenceNrs))
                    {
                        //TODO: This maybe should be an error. Better if the client uses a different prefix here.
                        addWarning($"Credit L{missingSequenceNr} is not using a pregenerated credit nr. This could cause dupe issues with future generated nrs!");
                    }
                }
            }
        }

        private SwedishMortgageLoanCreationRequest CreateLoanRequest(DataPointsParser dataPoints, Dictionary<int, int> customerIdByApplicantNr)
        {            
            var loanIndexes = dataPoints.GetGroupIndexes("loan", 1, null);

            if(loanIndexes.Count == 0)
            {
                dataPoints.AddError("No loan groups found");
                return null;
            }

            if (loanIndexes.Count > 1)
            {
                dataPoints.AddError("Currently only one loan group is supported");
                return null;
            }
                        
            var monthlyFeeAmount = dataPoints.RequireDecimal("shared", "monthlyFeeAmount");
            var loanEndDate = dataPoints.RequireDate("shared", "loanEndDate");
            var loanProviderName = dataPoints.RequireString("shared", "loanProviderName");
            var loanFundName = dataPoints.RequireString("shared", "loanFundName");

            var collateralType = dataPoints.RequireString("shared", "collateralType", mustBeOneOf: new List<string> { "seBrf", "seFastighet" });
            bool isBrfApartment = collateralType == "seBrf";
            var currentCombinedYearlyIncomeAmount = dataPoints.RequireDecimal("shared", "currentCombinedYearlyIncomeAmount");
            var otherMortageLoansAmount = dataPoints.RequireDecimal("shared", "otherMortageLoansAmount");
            var objectValue = dataPoints.RequireDecimal("shared", "objectValue", isValid: x => x > 0m);
            var loans = loanIndexes.Select(x =>
            {
                var loanGroupName = $"loan {x}";
                return new
                {
                    CreditNr = dataPoints.RequireString(loanGroupName, "creditNr"),
                    CurrentLoanAmount = dataPoints.RequireDecimal(loanGroupName, "currentLoanAmount"),
                    AmortBasisCurrentLoanAmount = dataPoints.RequireDecimal(loanGroupName, "amortBasisCurrentLoanAmount"),
                    AmortBasisMaxLoanAmount = dataPoints.RequireDecimal(loanGroupName, "amortBasisMaxLoanAmount"),
                    AmortBasisRuleCode = dataPoints.RequireString(loanGroupName, "amortBasisRuleCode", mustBeOneOf: new List<string> { "none", "r201616", "r201723" }),
                    MonthlyAmortizationAmount = dataPoints.RequireDecimal(loanGroupName, "monthlyAmortizationAmount"),
                    InterestRebindMounthCount = int.Parse(dataPoints.GetString(loanGroupName, "interestRebindMounthCount", isRequired: true, isValid: y => int.TryParse(y, out var _)) ?? "0"),
                    NextInterestRebindDate = dataPoints.RequireDate(loanGroupName, "nextInterestRebindDate"),
                    NominalInterestRatePercent = dataPoints.RequireDecimal(loanGroupName, "nominalInterestRatePercent"),
                    ReferenceInterestRate = dataPoints.RequireDecimal(loanGroupName, "referenceInterestRate")                    
                };
            }).ToList();

            if(loans.Count != loans.Select(x => x.CreditNr).Distinct().Count())
            {
                dataPoints.AddError("There are duplicate entries for creditNr");
                return null;
            }

            (decimal? Lti, decimal? Ltv) ComputeLtiAndLtv()
            {
                var currentLoansBalance = loans.Sum(x => x.CurrentLoanAmount);
                var ltiFraction = SwedishMortgageLoanAmortizationBasisService.ComputeLti(currentCombinedYearlyIncomeAmount, currentLoansBalance, otherMortageLoansAmount);
                var ltvFraction = SwedishMortgageLoanAmortizationBasisService.ComputeLtv(objectValue, currentLoansBalance);
                return (Lti: ltiFraction, Ltv: ltvFraction);
            }

            var ltiAndLtv = ComputeLtiAndLtv();

            var r = new SwedishMortgageLoanCreationRequest
            {
                NewCollateral = new SwedishMortgageLoanCreationRequest.CollateralModel
                {
                    AddressCity = dataPoints.RequireString("shared", "addressCity"),
                    AddressMunicipality = dataPoints.RequireString("shared", "addressMunicipality"),
                    AddressStreet = dataPoints.RequireString("shared", "addressStreet"),
                    AddressZipcode = dataPoints.RequireString("shared", "addressZipcode"),
                    IsBrfApartment = isBrfApartment,
                    ObjectId = dataPoints.GetString("shared", "objectId", isRequired: !isBrfApartment, isForbidden: isBrfApartment),
                    BrfName = dataPoints.GetString("shared", "brfName", isRequired: isBrfApartment, isForbidden: !isBrfApartment),
                    BrfOrgNr = dataPoints.GetString("shared", "brfOrgNr", isRequired: isBrfApartment, isForbidden: !isBrfApartment, isValid: OrganisationNumberSe.IsValid),
                    BrfApartmentNr = dataPoints.GetString("shared", "brfApartmentNr", isRequired: isBrfApartment, isForbidden: !isBrfApartment),
                    TaxOfficeApartmentNr = dataPoints.GetString("shared", "taxOfficeApartmentNr", isRequired: isBrfApartment, isForbidden: !isBrfApartment)
                },
                AmortizationBasis = new Models.SwedishMortgageLoanAmortizationBasisModel
                {
                    CurrentCombinedYearlyIncomeAmount = currentCombinedYearlyIncomeAmount,
                    OtherMortageLoansAmount = otherMortageLoansAmount,
                    ObjectValue = objectValue,
                    ObjectValueDate = dataPoints.RequireDate("shared", "objectValueDate", isValid: x => x <= clock.Today),
                    LtiFraction = ltiAndLtv.Lti,
                    LtvFraction = ltiAndLtv.Ltv,
                    Loans = loans.Select(x => new Models.SwedishMortgageLoanAmortizationBasisModel.LoanModel
                    {
                        CreditNr = x.CreditNr,
                        IsUsingAlternateRule = false, //TODO: Support this
                        CurrentCapitalBalanceAmount = x.AmortBasisCurrentLoanAmount,
                        MaxCapitalBalanceAmount = x.AmortBasisMaxLoanAmount,
                        RuleCode = x.AmortBasisRuleCode,
                        MonthlyAmortizationAmount = x.MonthlyAmortizationAmount
                    }).ToList()
                },
                Loans = loans.Select(x => new SwedishMortgageLoanCreationRequest.SeMortgageLoanModel
                {
                    CreditNr = x.CreditNr,
                    LoanAmount = x.CurrentLoanAmount,
                    Applicants = customerIdByApplicantNr.Keys.Select(applicantNr => new nCredit.DbModel.BusinessEvents.MortgageLoanRequest.Applicant
                    {
                        ApplicantNr = applicantNr,
                        AgreementPdfArchiveKey = null, //TODO: Support an agreement
                        CustomerId = customerIdByApplicantNr[applicantNr],
                        OwnershipPercent = customerIdByApplicantNr.Keys.Count == 2 ? 50 : 100
                    }).ToList(),
                    FixedMonthlyAmortizationAmount = x.MonthlyAmortizationAmount,
                    MonthlyFeeAmount = monthlyFeeAmount,
                    PropertyOwnerCustomerIds = customerIdByApplicantNr.Keys.Select(applicantNr => customerIdByApplicantNr[applicantNr]).ToList(),
                    EndDate = loanEndDate,
                    ProviderName = loanProviderName,
                    LoanOwnerName = loanFundName,
                    InterestRebindMounthCount = x.InterestRebindMounthCount,
                    NextInterestRebindDate = x.NextInterestRebindDate,
                    NominalInterestRatePercent = x.NominalInterestRatePercent,
                    ReferenceInterestRate = x.ReferenceInterestRate,
                    //Not implemented yet
                    ActiveDirectDebitAccount = null, //TODO: support direct debit
                    AmortizationExceptionReasons = null, //TODO: Support amortization exception
                    AmortizationExceptionUntilDate = null,
                    ExceptionAmortizationAmount = null,
                    KycQuestionsJsonDocumentArchiveKey = null,
                    ApplicationNr = null,
                    DrawnFromLoanAmountInitialFeeAmount = null,
                    ProviderApplicationId = null,
                    ConsentingPartyCustomerIds = null,
                    Documents = null
                }).ToList()
            };            

            return r;
        }

        private Dictionary<int, CreateOrUpdatePersonRequest> GetCustomers(DataPointsParser dataPoints)
        {
            var result = new Dictionary<int, CreateOrUpdatePersonRequest>();
            var customerIndexes = dataPoints.GetGroupIndexes("customer", 1, 2);
            foreach(var customerIndex in customerIndexes)
            {
                var groupName = $"customer {customerIndex}";
                var customer = new CreateOrUpdatePersonRequest
                {
                    CivicRegNr = dataPoints.GetString(groupName, "civicRegNr", isRequired: true, isValid: CivicRegNumberSe.IsValid),
                    Properties = new List<CreateOrUpdatePersonRequest.Property>()
                };
                void AddProperty(string name, Func<string, string> valueByName) => customer.Properties.Add(new CreateOrUpdatePersonRequest.Property { Name = name, Value = valueByName(name) });

                AddProperty("phone", x => dataPoints.RequireString(groupName, x));
                AddProperty("email", x => dataPoints.GetString(groupName, x, isRequired: true, isValid: y => y.Contains("@")));
                AddProperty("firstName", x => dataPoints.RequireString(groupName, x));
                AddProperty("lastName", x => dataPoints.RequireString(groupName, x));
                AddProperty("addressStreet", x => dataPoints.RequireString(groupName, x));
                AddProperty("addressZipcode", x => dataPoints.RequireString(groupName, x));
                AddProperty("addressCity", x => dataPoints.RequireString(groupName, x));
                result[customerIndex] = customer;
            }
            return result;
        }

        private DataPointsParser ParseDataPoints(List<List<string>> rows, Action<string> addError)
        {
            var dataPoints = new Dictionary<string, Dictionary<string, string>>();
            var nonHeaderRows = rows.Skip(1).ToList();

            bool IsEmptyRow(List<string> cells) => cells.All(x => string.IsNullOrWhiteSpace(x));

            //Remove trailing empty rows
            while (IsEmptyRow(nonHeaderRows.Last()))
                nonHeaderRows.RemoveAt(nonHeaderRows.Count - 1);
            
            foreach (var row in nonHeaderRows.Select((x, i) => new { Cells = x, RowNr = i + 2 }))
            {
                var cells = row.Cells;
                if(cells == null || cells.Count < 3 || string.IsNullOrWhiteSpace(cells[0]) || string.IsNullOrWhiteSpace(cells[1]))
                {
                    addError($"Row {row.RowNr} has < 3 columns or is missing the group or name column");
                    continue;
                }
                var groupName = cells[0];
                var group = dataPoints.Ensure(groupName, () => new Dictionary<string, string>());
                var name = cells[1];
                if (group.ContainsKey(name))
                    addError($"Row {row.RowNr} contains a duplicate data point");
                else
                    group[name] = cells[2];
            }

            return new DataPointsParser(dataPoints, addError, clock);
        }

        private class DataPointsParser
        {
            private readonly Dictionary<string, Dictionary<string, string>> dataPoints;
            private readonly Action<string> addError;
            private readonly ICoreClock clock;

            public DataPointsParser(Dictionary<string, Dictionary<string, string>> dataPoints, Action<string> addError, ICoreClock clock)
            {
                this.dataPoints = dataPoints;
                this.addError = addError;
                this.clock = clock;
            }

            public Dictionary<string, Dictionary<string, string>> Raw => dataPoints;
            public void AddError(string error) => addError(error);

            public string GetString(string groupName, string name, List<string> mustBeOneOf = null, bool isRequired = false, bool isForbidden = false, Func<string, bool> isValid = null)
            {
                var value = dataPoints?.Opt(groupName)?.Opt(name);
                if (isRequired && string.IsNullOrWhiteSpace(value))
                {
                    addError($"{groupName}.{name} required");
                    return null;
                }
                if (isForbidden && !string.IsNullOrWhiteSpace(value))
                {
                    addError($"{groupName}.{name} forbidden");
                    return null;
                }
                if (mustBeOneOf != null && !value.IsOneOf(mustBeOneOf.ToArray()))
                {
                    addError($"{groupName}.{name} must be one of {(string.Join("|", mustBeOneOf))}");
                    return null;
                }

                value = value?.Trim();

                if (!string.IsNullOrWhiteSpace(value) && isValid != null && !isValid(value))
                {
                    addError($"{groupName}.{name} is invalid");
                    return null;
                }

                return string.IsNullOrWhiteSpace(value) ? null : value?.Trim();
            }
            public string RequireString(string groupName, string name, List<string> mustBeOneOf = null) =>
                GetString(groupName, name, mustBeOneOf: mustBeOneOf, isRequired: true);
            public decimal RequireDecimal(string groupName, string name, Func<decimal, bool> isValid = null)
            {
                var rawValue = RequireString(groupName, name);
                if (rawValue == null)
                    return 0m; //NOTE: Error handled already by RequireString
                var value = Numbers.ParseDecimalOrNull(rawValue);
                if (!value.HasValue)
                {
                    addError($"{groupName}.{name} invalid decimal number");
                    return 0m;
                }
                else if (isValid != null && !isValid(value.Value))
                {
                    addError($"{groupName}.{name} invalid");
                    return 0m;
                }
                return value.Value;
            }

            public DateTime RequireDate(string groupName, string name, Func<DateTime, bool> isValid = null)
            {
                var rawValue = RequireString(groupName, name);
                if (rawValue == null)
                    return clock.Today; //NOTE: Error handled already by RequireString
                var value = Dates.ParseDateTimeExactOrNull(rawValue, "yyyy-MM-dd");
                if (!value.HasValue)
                {
                    addError($"{groupName}.{name} invalid date (YYYY-MM-DD)");
                    return clock.Today; //NOTE: Will never actually be used since an error is registered. Just to get through the parsing to find other errors.
                }
                else if (isValid != null && !isValid(value.Value))
                {
                    addError($"{groupName}.{name} invalid");
                    return clock.Today;
                }
                return value.Value;
            }

            public List<int> GetGroupIndexes(string groupPrefix, int minAllowedIndex, int? maxAllowedIndex)
            {
                List<int> indexes = new List<int>();
                foreach (var groupName in dataPoints.Keys.Where(x => x.StartsWith(groupPrefix)))
                {
                    if (groupName.Length < (groupPrefix.Length + 2))
                        addError($"Invalid {groupPrefix} group name '{groupName}'. Should be '{groupPrefix} <index>'");
                    var index = Numbers.ParseInt32OrNull(groupName.Substring(groupPrefix.Length).Trim());
                    if (!index.HasValue)
                        addError($"Invalid {groupPrefix} group name '{groupName}'. Should be '{groupPrefix} <index>'");
                    else if (index.Value < minAllowedIndex)
                        addError($"{groupPrefix} group index must be >= {minAllowedIndex})");
                    else if (maxAllowedIndex.HasValue && index.Value > maxAllowedIndex.Value)
                        addError($"{groupPrefix} group index must be < {maxAllowedIndex.Value})");
                    else
                        indexes.Add(index.Value);
                }
                return indexes.DistinctPreservingOrder().ToList();
            }
        }
    }

    public class SwedishMortgageLoanImportRequest
    {
        [Required]
        public string Base64EncodedExcelFile { get; set; }
        [Required]
        public string FileName { get; set; }

        [Required]
        public bool? IsPreviewOnly { get; set; }

        public string AgreementArchiveKey { get; set; }

        public string AgreementFileName { get; set; }

        public string AgreementFileBase64Data { get; set; }
    }

    public class SwedishMortgageLoanImportResponse
    {
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }
        public Dictionary<string, Dictionary<string, string>> RawDataPoints { get; set; }        
        public SwedishMortgageLoanCreationRequest CreatePreview { get; set; }
        public List<CreateOrUpdatePersonRequest> CustomersPreview { get; set; }
        public SwedishAmorteringsunderlag AmorteringsunderlagPreview { get; set; }
        public SwedishMortgageLoanCreationResponse LoansCreated { get; set; }
        public bool IsCreateAllowed { get; set; }
        public string PreviewAgreementArchiveKey { get; set; }
    }
}
