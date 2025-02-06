
using NTech.Banking.BankAccounts.Se;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace NTech.Core.Credit.Shared.Models
{
    public partial class SwedishMortgageLoanCreationRequest : IValidatableObject
    {
        private ValidationResult Err(string errorMessage, params string[] memberNames) => new ValidationResult(errorMessage, memberNames);

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if ((NewCollateral != null && ExistingCollateralId.HasValue) || (NewCollateral == null && !ExistingCollateralId.HasValue))
                yield return Err("NewCollateral XOR ExistingCollateralId required", nameof(NewCollateral), nameof(ExistingCollateralId));
            if ((Loans?.Count ?? 0) < 1)
                yield return Err("At least one loan must be present", nameof(Loans));
            if((AmortizationBasis?.Loans?.Count ?? 0) < 1)
                yield return Err("At least one loan must be present in AmortizationBasis");

            if(Loans != null && Loans.Count > 0)
            {
                var nrOfApplicants = Loans[0].Applicants.Count;
                if (nrOfApplicants == 0)
                    yield return Err("At least one Applicant must be present on each loan");
                else
                {
                    var expectedApplicantNrs = Enumerable.Range(1, nrOfApplicants);
                    foreach (var loan in Loans)
                    {
                        var actualApplicantNrs = loan.Applicants.Select(x => x.ApplicantNr).ToHashSetShared();
                        if (!actualApplicantNrs.SetEquals(expectedApplicantNrs))
                            yield return Err("ApplicantNr should be 1, 2 and so on up to the nr of elements in Applicants", "ApplicantNr");

                        foreach (var applicant in loan.Applicants)
                        {
                            if (applicant.CustomerId <= 0)
                                yield return Err("Customer must be > 0", "CustomerId");
                        }

                        if (loan.ActiveDirectDebitAccount != null)
                        {
                            var d = loan.ActiveDirectDebitAccount;
                            if (d.BankAccountNrOwnerApplicantNr < 1 || d.BankAccountNrOwnerApplicantNr > nrOfApplicants)
                                yield return Err($"Direct Debit BankAccountNrOwnerApplicantNr must be one 1 ... {nrOfApplicants}", "BankAccountNrOwnerApplicantNr");
                            if (string.IsNullOrWhiteSpace(d.BankAccountNr) || !BankAccountNumberSe.TryParse(d.BankAccountNr, out var _, out var __))
                                yield return Err("Direct Debit BankAccountNr must be a valid swedish bank account nr");
                        }
                    }
                }
            }
        }
    }
}