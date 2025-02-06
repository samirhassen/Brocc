using nCredit;
using nCredit.DbModel.BusinessEvents;
using Newtonsoft.Json;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace NTech.Core.Credit.Shared.Services
{
    public class MortageLoanOwnerManagementService : BusinessEventManagerOrServiceBase
    {
        private readonly CreditContextFactory creditContextFactory;
        private readonly CachedSettingsService cachedSettingsService;

        public MortageLoanOwnerManagementService(INTechCurrentUserMetadata currentUser, CreditContextFactory creditContextFactory, ICoreClock clock,
            IClientConfigurationCore clientConfiguration,
            ICreditEnvSettings envSettings,
            CachedSettingsService cachedSettingsService) : base(currentUser, clock, clientConfiguration)
        {
            this.creditContextFactory = creditContextFactory;
            this.cachedSettingsService = cachedSettingsService;

            if (!IsEnabled(clientConfiguration, envSettings))
                throw new NTechCoreWebserviceException("Can only be used for standard mortage loans in sweden");
        }

        public static bool IsEnabled(IClientConfigurationCore clientConfiguration, ICreditEnvSettings envSettings)
        {
            return envSettings.IsStandardMortgageLoansEnabled && clientConfiguration.Country.BaseCountry == "SE";
        }

        public LoanOwnerManagementResponse FetchMortgageLoanOwners(string creditNr)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var loanOwner = creditNr != null ? context.DatedCreditStringsQueryable
                    .Where(x => x.CreditNr == creditNr && x.Name == DatedCreditStringCode.LoanOwner.ToString())
                    .OrderByDescending(y => y.ChangedDate)
                    .ThenByDescending(z => z.BusinessEventId)
                    .Select(creditString => creditString.Value)
                    .FirstOrDefault() : null; 
            
                return new LoanOwnerManagementResponse
                {
                    LoanOwnerName = loanOwner,
                    AvailableLoanOwnerOptions = GetAvailableLoanOwners()
                };
            }
        }

        public LoanOwnerManagementResponse EditOwner(LoanOwnerManagementRequest request)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var evt = AddBusinessEvent(BusinessEventType.ChangedMortgageLoanOwner, context);
                var credit = context.CreditHeadersQueryable.FirstOrDefault(x => x.CreditNr == request.CreditNr) ?? throw new Exception($"Credit with creditNr '{request.CreditNr}' does not exist.");
                AddDatedCreditString(DatedCreditStringCode.LoanOwner.ToString(), request.LoanOwnerName, credit, evt, context);
                AddComment($"Loan owner changed to '{request.LoanOwnerName}.'", BusinessEventType.ChangedMortgageLoanOwner, context, creditNr: credit.CreditNr, evt: evt);

                context.SaveChanges();
                
                return new LoanOwnerManagementResponse
                {
                    LoanOwnerName = request.LoanOwnerName
                };
            }
        }

        public LoanOwnerManagementResponse BulkEditOwner(BulkEditOwnerRequest request)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                if (!IsValid(context, request.CreditNrs, request.LoanOwnerName, out string validationErrorMessage))
                {
                    throw new Exception($"Validation error: {validationErrorMessage}"); 
                };

                foreach (var creditNr in request.CreditNrs)
                {
                    var evt = AddBusinessEvent(BusinessEventType.BulkChangedMortgageLoanOwner, context);
                    var credit = context.CreditHeadersQueryable.FirstOrDefault(x => x.CreditNr == creditNr) ?? throw new Exception($"Credit with creditNr '{creditNr}' does not exist.");
                    AddDatedCreditString(DatedCreditStringCode.LoanOwner.ToString(), request.LoanOwnerName, credit, evt, context);
                    AddComment($"Loan owner bulk changed to '{request.LoanOwnerName}'.", BusinessEventType.BulkChangedMortgageLoanOwner, context, creditNr: credit.CreditNr, evt: evt);
                };

                context.SaveChanges(); 

                return new LoanOwnerManagementResponse
                {
                    LoanOwnerName = request.LoanOwnerName
                };
            }
        }

        public BulkEditLoanOwnerPreviewResponse GetBulkEditPreview(BulkEditOwnerPreviewRequest request)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var creditNrs = request.CreditNrs;
                
                if (!IsValid(context, creditNrs, request.LoanOwnerName, out string validationErrorMessage))
                {
                    return new BulkEditLoanOwnerPreviewResponse
                    {
                        IsValid = false, 
                        ValidationErrorMessage = validationErrorMessage
                    }; 
                }

                return new BulkEditLoanOwnerPreviewResponse
                {
                    IsValid = true, 
                    NrOfLoansEdit = creditNrs.Count,
                    LoanOwnerName = request.LoanOwnerName
                };
            }
        }

        private bool IsValid(ICreditContextExtended context, List<string> creditNrs, string loanOwnerName, out string validationErrorMessage)
        {
            var distinctCreditNrs = creditNrs.Distinct().ToList();
            if (distinctCreditNrs.Count != creditNrs.Count)
            {
                validationErrorMessage = "Duplicate loans.";
                return false;
            }

            var matchingCreditCount = context.CreditHeadersQueryable.Count(x => creditNrs.Contains(x.CreditNr));
            if (matchingCreditCount != creditNrs.Count)
            {
                validationErrorMessage = "One or more loans do not exist."; 
                return false; 
            }

            var availableLoanOwners = GetAvailableLoanOwners();
            if (!availableLoanOwners.Contains(loanOwnerName))
            {
                validationErrorMessage = "Loan owner does not exist.";
                return false;
            }

            validationErrorMessage = ""; 
            return true; 
        }

        private string[] GetAvailableLoanOwners()
        {
            var loanOwnerManagementSettings = cachedSettingsService.LoadSettings("loanOwnerManagement");
            List<string> allLoanOwnerNames = JsonConvert.DeserializeObject<List<string>>(loanOwnerManagementSettings["listOfNames"]);

            return allLoanOwnerNames.ToArray();
        }
    }

    public class LoanOwnerManagementRequest
    {
        public string CreditNr { get; set; }
        public string LoanOwnerName { get; set; }
    }

    public class LoanOwnerManagementResponse
    {
        public string LoanOwnerName { get; set; }
        public string[] AvailableLoanOwnerOptions { get; set; }
    }

    public class BulkEditOwnerRequest
    {
        [Required]
        public List<string> CreditNrs { get; set; }
        [Required]
        public string LoanOwnerName { get; set; }
    }

    public class BulkEditOwnerPreviewRequest {
        [Required]
        public List<string> CreditNrs { get; set; }
        [Required]
        public string LoanOwnerName { get; set; }
    }

    public class BulkEditLoanOwnerPreviewResponse
    {
        public string LoanOwnerName { get; set; }
        public int NrOfLoansEdit { get; set; }
        public bool IsValid { get; set; }
        public string ValidationErrorMessage { get; set; }
    }
}
