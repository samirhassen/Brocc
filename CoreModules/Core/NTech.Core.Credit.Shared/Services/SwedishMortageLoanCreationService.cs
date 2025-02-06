using nCredit;
using nCredit.Code;
using nCredit.Code.Services;
using nCredit.DbModel.BusinessEvents;
using nCredit.DomainModel;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Credit.Shared.Models;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Infrastructure.CoreValidation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace NTech.Core.Credit.Shared.Services
{
    public class SwedishMortageLoanCreationService
    {
        private readonly NewMortgageLoanBusinessEventManager mgr;
        private readonly CreditContextFactory creditContextFactory;
        private readonly ICoreClock clock;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly ICreditEnvSettings envSettings;
        private readonly MortgageLoanCollateralService collateralService;
        private readonly CustomerRelationsMergeService customerRelationsMergeService;

        public SwedishMortageLoanCreationService(NewMortgageLoanBusinessEventManager newMortgageLoanBusinessEventManager, CreditContextFactory creditContextFactory,
            ICoreClock clock, IClientConfigurationCore clientConfiguration, ICreditEnvSettings envSettings, MortgageLoanCollateralService collateralService,
            CustomerRelationsMergeService customerRelationsMergeService)
        {
            this.mgr = newMortgageLoanBusinessEventManager;
            this.creditContextFactory = creditContextFactory;
            this.clock = clock;
            this.clientConfiguration = clientConfiguration;
            this.envSettings = envSettings;
            this.collateralService = collateralService;
            this.customerRelationsMergeService = customerRelationsMergeService;
            if (!IsEnabled(clientConfiguration, envSettings))
                throw new NTechCoreWebserviceException("Can only be used for standard mortage loans in sweden");
        }

        public static bool IsEnabled(IClientConfigurationCore clientConfiguration, ICreditEnvSettings envSettings)
        {            
            return envSettings.IsStandardMortgageLoansEnabled && clientConfiguration.Country.BaseCountry == "SE";
        }

        public SwedishMortgageLoanCreationResponse CreateLoans(SwedishMortgageLoanCreationRequest request)
        {
            var result = CreateLoansInternal(request);
            if (result.CreditNrs?.Any() == true)
            {
                customerRelationsMergeService.MergeLoansToCustomerRelations(onlyTheseCreditNrs: result.CreditNrs.ToHashSetShared());
            }

            return result;
        }

        //Validation that requires database or the clock that cant be done using the normal validation attributes.
        //If possible, add new validation as attributes or to IValidatableObject. Only put it here if it actually needs db or clock access or similar.
        private void Validate(SwedishMortgageLoanCreationRequest request, ICreditContextExtended context)
        {            
            var today = clock.Today;

            ValidationResult Err(string errorMessage, params string[] memberNames) => new ValidationResult(errorMessage, memberNames);
            ValidationResult FutureDate(DateTime? d, string memberName)
            {
                if (!d.HasValue)
                    return null;
                return d.Value >= today ? null : Err($"Must be >= {today:yyyy-MM-dd} (today)", memberName);
            }
                
            ValidationResult HistoricalDate(DateTime? d, string memberName)
            {
                if (!d.HasValue)
                    return null;
                return d.Value <= today ? null : Err($"Must be <= {today:yyyy-MM-dd} (today)", memberName);
            }      

            IEnumerable<ValidationResult> ValidateInternal()
            {
                if(!string.IsNullOrWhiteSpace(request.AgreementNr))
                {
                    if (!CreditFeatureToggles.IsAgreementNrEnabled(clientConfiguration))
                    {
                        yield return Err("AgreementNr is not enabled");
                    }
                    if(!CreditFeatureToggles.IsCoNotificationEnabled(clientConfiguration))
                    {
                        yield return Err("Co notification must be enabled to use AgreementNr");
                        if (request.AgreementNr.Any(x => !char.IsLetterOrDigit(x) && x != '-')) //This is a bit arbitrary but try to at least make sure this can be a filename or a key in an azure/aws bucket for possible future usecases.
                            yield return Err("AgreementNr can only contain letters, digits or -");
                        if(context.DatedCreditStringsQueryable.Any(x => x.Name == DatedCreditStringCode.MortgageLoanAgreementNr.ToString() && x.Value == request.AgreementNr))
                            yield return Err("AgreementNr already exists.");
                    }
                }
                if (request.ExistingCollateralId.HasValue && !context.CollateralHeadersQueryable.Any(x => x.Id == request.ExistingCollateralId.Value))
                    yield return Err("No such collateral exists", nameof(request.ExistingCollateralId));
                
                var creditNrs = request.Loans.Select(x => x.CreditNr).ToHashSetShared();
                if(request.Loans.Count != creditNrs.Count)
                    yield return Err("Duplicate value detected", "CreditNr");
                else if(context.CreditHeadersQueryable.Any(x => creditNrs.Contains(x.CreditNr)))
                    yield return Err("Loan already exists", "CreditNr");

                //NOTE: Should we check that all credit nrs are L<....> where <....> must exist in the sequence table?
                var providerNames = envSettings.GetAffiliateModels().Select(x => x.ProviderName).ToHashSetShared();
                foreach (var loan in request.Loans)
                {
                    yield return FutureDate(loan.AmortizationExceptionUntilDate, "AmortizationExceptionUntilDate");
                    yield return FutureDate(loan.EndDate, "EndDate");

                    if (!providerNames.Contains(loan.ProviderName))
                        yield return Err("No such provider exists", "ProviderName");

                    yield return FutureDate(loan.NextInterestRebindDate, "NextInterestRebindDate");

                    if(!request.AmortizationBasis.Loans.Any(x => x.CreditNr == loan.CreditNr))
                        yield return Err($"Loan {loan.CreditNr} is missing from AmortizationBasis.Loans", "AmortizationBasis");
                }

                var b = request.AmortizationBasis;

                yield return HistoricalDate(b.ObjectValueDate, "ObjectValueDate");
            }

            var errors = ValidateInternal().Where(x => x != null).ToList();
            if(errors.Any())
            {
                throw new MultiValidationException(errors.First().ErrorMessage, errors);
            }
        }

        private SwedishMortgageLoanCreationResponse CreateLoansInternal(SwedishMortgageLoanCreationRequest request)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                Validate(request, context);

                context.BeginTransaction();
                try
                {
                    var evt = mgr.AddBusinessEvent(BusinessEventType.NewMortgageLoan, context);
                    List<CreditHeader> createdLoans = new List<CreditHeader>();
                    CollateralHeader collateral;

                    var amortizationBasis = request.AmortizationBasis;

                    if (request.ExistingCollateralId.HasValue)
                    {
                        if (amortizationBasis != null)
                        {
                            collateral = collateralService.UpdateExistingCollateralWithNewSeAmortizationBasis(context, request.ExistingCollateralId.Value, amortizationBasis, evt);
                        }
                        else
                        {
                            collateral = context.CollateralHeadersQueryable.Single(x => x.Id == request.ExistingCollateralId.Value);
                        }
                    }
                    else if (request.NewCollateral != null)
                    {
                        var items = new Dictionary<string, string>();
                        void AddItem(string name, string value)
                        {
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                items[name] = value.Trim();
                            }
                        }
                        var m = request.NewCollateral;
                        if (m.IsBrfApartment)
                        {
                            AddItem("objectTypeCode", "seBrf");
                            AddItem("seBrfOrgNr", m.BrfOrgNr);
                            AddItem("seBrfName", m.BrfName);
                            AddItem("seBrfApartmentNr", m.BrfApartmentNr);
                            AddItem("seTaxOfficeApartmentNr", m.TaxOfficeApartmentNr);
                        }
                        else
                        {
                            AddItem("objectTypeCode", "seFastighet");
                            AddItem("objectId", m.ObjectId);
                        }
                        AddItem("objectAddressStreet", m.AddressStreet);
                        AddItem("objectAddressZipcode", m.AddressZipcode);
                        AddItem("objectAddressCity", m.AddressCity);
                        AddItem("objectAddressMunicipality", m.AddressMunicipality);
                        collateral = collateralService.CreateCollateral(context, items, existingEvent: evt, seAmortizationBasis: amortizationBasis);
                    }
                    else
                    {
                        throw new NTechCoreWebserviceException("Exactly one of ExistingCollateralId and NewCollateral must be specified") { ErrorHttpStatusCode = 400, IsUserFacing = true };
                    }

                    IOcrNumber sharedOcrPaymentReference = null;
                    if(!string.IsNullOrWhiteSpace(request.AgreementNr))
                    {
                        sharedOcrPaymentReference = new OcrPaymentReferenceGenerator(clientConfiguration, creditContextFactory).GenerateNew();
                    }

                    var referenceInterestRate = new Lazy<decimal>(() => new SharedDatedValueDomainModel(context).GetReferenceInterestRatePercent(clock.Today));
                    foreach (var loanRequest in request.Loans)
                    {
                        var createdLoan = mgr.CreateNewMortgageLoan(context, new MortgageLoanRequest
                        {
                            ActiveDirectDebitAccount = loanRequest.ActiveDirectDebitAccount,
                            ActualAmortizationAmount = loanRequest.FixedMonthlyAmortizationAmount,
                            AmortizationExceptionReasons = loanRequest.AmortizationExceptionReasons,
                            AmortizationExceptionUntilDate = loanRequest.AmortizationExceptionUntilDate,
                            ExceptionAmortizationAmount = loanRequest.ExceptionAmortizationAmount,
                            MonthlyFeeAmount = loanRequest.MonthlyFeeAmount,
                            LoanAmount = loanRequest.LoanAmount,
                            Applicants = loanRequest.Applicants, //Note that customers need to be created by api before calling this to have customer ids
                            KycQuestionsJsonDocumentArchiveKey = loanRequest.KycQuestionsJsonDocumentArchiveKey,
                            ApplicationNr = loanRequest.ApplicationNr,
                            NrOfApplicants = loanRequest.Applicants.Count,
                            ProviderApplicationId = loanRequest.ProviderApplicationId,
                            CreditNr = loanRequest.CreditNr,
                            Documents = loanRequest.Documents,
                            SettlementDate = clock.Today,
                            ProviderName = loanRequest.ProviderName,
                            EndDate = loanRequest.EndDate,
                            InterestRebindMounthCount = loanRequest.InterestRebindMounthCount,
                            NextInterestRebindDate = loanRequest.NextInterestRebindDate,
                            NominalInterestRatePercent = loanRequest.NominalInterestRatePercent,
                            ReferenceInterestRate = loanRequest.ReferenceInterestRate,
                            ConsentingPartyCustomerIds = loanRequest.ConsentingPartyCustomerIds,
                            PropertyOwnerCustomerIds = loanRequest.PropertyOwnerCustomerIds,
                            DrawnFromLoanAmountInitialFees = loanRequest.DrawnFromLoanAmountInitialFeeAmount.HasValue
                                ? new List<MortgageLoanRequest.AmountModel>
                                {
                                    new MortgageLoanRequest.AmountModel
                                    {
                                        Amount = loanRequest.DrawnFromLoanAmountInitialFeeAmount.Value,
                                    }
                                }
                                : null,
                            LoanOwnerName = loanRequest.LoanOwnerName,
                            SharedOcrPaymentReference = sharedOcrPaymentReference?.NormalForm,
                            MortgageLoanAgreementNr = request.AgreementNr,
                            FirstNotificationCosts = loanRequest
                                ?.FirstNotificationCosts
                                ?.Select(y => new MortgageLoanRequest.FirstNotificationCostItem 
                                { 
                                    CostCode = y.CostCode, 
                                    CostAmount = y.CostAmount 
                                })
                                ?.ToList(),
                            //Things may have to be added in some form
                            CapitalizedInitialFees = null,
                            LoanAmountParts = null,
                            Collaterals = null,
                            HistoricalStartDate = null
                        }, referenceInterestRate, existingEvent: evt, existingCollateral: collateral);
                        createdLoans.Add(createdLoan);
                    }

                    context.SaveChanges();
                    context.CommitTransaction();

                    return new SwedishMortgageLoanCreationResponse
                    {
                        CollateralId = collateral.Id,
                        CreditNrs = createdLoans.Select(x => x.CreditNr).ToList()
                    };
                }
                catch
                {
                    context.RollbackTransaction();
                    throw;
                }
            }
        }
    }

    public class SwedishMortgageLoanCreationResponse
    {
        public List<string> CreditNrs { get; set; }
        public int CollateralId { get; set; }
    }
}
