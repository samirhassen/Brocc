using System.ComponentModel.DataAnnotations;

namespace NTech.Core.Module.Shared.Infrastructure.CoreValidation
{
    public abstract class NumbericAttribueBase : ValidationAttribute
    {
        protected abstract string GetCustomErrorMessage(decimal value, string formattedValue);
        protected abstract bool IsValidNumber(decimal value);

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {            
            if (value is int || value is int?)
            {
                var v = value as int?;                
                if (v.HasValue)
                    return DoValidate(v.Value);
                    
            }
            else if (value is long || value is long?)
            {
                var v = value as long?;
                if (v.HasValue)
                    return DoValidate(v.Value);
            }
            else if (value is decimal || value is decimal?)
            {
                var v = value as decimal?;
                if (v.HasValue)
                    return DoValidate(v.Value);
            }
            return ValidationResult.Success;
        }

        private ValidationResult DoValidate(decimal value)
        {
            if (!IsValidNumber(value))
                return new ValidationResult(GetCustomErrorMessage(value, value.ToString("{0:0.##}")));
            else
                return ValidationResult.Success;
        }
    }
}
