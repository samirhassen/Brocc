using System.ComponentModel.DataAnnotations;

namespace NTech.Services.Infrastructure.NTechWs
{
    public abstract class NTechWsStringValidationAttributeBase : ValidationAttribute
    {
        public NTechWsStringValidationAttributeBase()
        {
        }

        public override bool IsValid(object value)
        {
            var valueActual = value as string;
            return string.IsNullOrWhiteSpace(valueActual) || IsValidString(valueActual);
        }

        /// <summary>
        /// Check if value is valid. Will never be called for null or whitespace values.
        /// Combine with Required to prevent empty values.
        /// </summary>
        protected abstract bool IsValidString(string value);
    }
}
