using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace NTech.Core.Module.Shared.Infrastructure.CoreValidation
{
    public class ListSizeRangeAttribute: ValidationAttribute
    {
        private readonly int minSize;
        private readonly int maxSize;

        public ListSizeRangeAttribute(int minSize, int maxSize)
        {
            this.minSize = minSize;
            this.maxSize = maxSize;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (!value.GetType().IsClass || value == null)
                return ValidationResult.Success;

            var list = value as System.Collections.IList;
            if(list == null)
                return ValidationResult.Success;

            if (list.Count < minSize || list.Count > maxSize)
                return new ValidationResult($"Length must be between {minSize} and {maxSize}", Enumerables.Singleton(validationContext.MemberName));

            return ValidationResult.Success; ;
        }
    }
}
