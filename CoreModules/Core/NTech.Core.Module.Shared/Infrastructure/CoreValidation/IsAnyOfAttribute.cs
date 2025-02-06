using System.ComponentModel.DataAnnotations;

namespace NTech.Core.Module.Shared.Infrastructure.CoreValidation
{
    public class IsAnyOfAttribute : ValidationAttribute
    {
        private readonly string[] allowedValues;
        private readonly bool ignoreCase;

        public IsAnyOfAttribute(string[] allowedValues, bool ignoreCase = false)
        {
            this.allowedValues = allowedValues;
            this.ignoreCase = ignoreCase;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            ValidationResult Err () => new ValidationResult($"Must be one of {string.Join("|", allowedValues)}");

            if (value == null)
                return ValidationResult.Success; //Handled by [Required]

            var vs = value as string;

            if (vs == null)
                return Err();

            var isValid = ignoreCase ? vs.IsOneOfIgnoreCase(allowedValues) : vs.IsOneOf(allowedValues);
            return isValid
                ? ValidationResult.Success
                : Err();
        }
    }
}
