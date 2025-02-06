using nCredit.DbModel.BusinessEvents;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace nCredit.WebserviceMethods.MortgageLoan
{
    public class ValidatableMortgageLoanRequest : MortgageLoanRequest, IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var request = ((MortgageLoanRequest)validationContext.ObjectInstance) ?? new MortgageLoanRequest();

            if (string.IsNullOrWhiteSpace(request.CreditNr))
                yield return new ValidationResult("CreditNr required");

            if (request.NrOfApplicants == 0)
                yield return new ValidationResult("NrOfApplicants required");

            if (NEnv.IsStandardMortgageLoansEnabled)
            {
                if (request?.Collaterals != null)
                    yield return new ValidationResult("Collaterals not allowed for standard mortagage loans");
                if (!request.CollateralId.HasValue)
                    yield return new ValidationResult("CollateralId is required for standard mortgage loans");
            }
            else
            {
                if ((request?.ConsentingPartyCustomerIds?.Count ?? 0) > 0 || (request?.PropertyOwnerCustomerIds?.Count ?? 0) > 0)
                    yield return new ValidationResult("ConsentingPartyCustomerIds and PropertyOwnerCustomerIds are only allowed for standard mortgage loans");
            }
        }
    }
}