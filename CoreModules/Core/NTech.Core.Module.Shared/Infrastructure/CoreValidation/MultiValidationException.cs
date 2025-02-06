using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NTech.Core.Module.Shared.Infrastructure.CoreValidation
{
    //The builtin ValidationException just done one single ValidationResult
    public class MultiValidationException : Exception
    {
        public MultiValidationException(string message, List<ValidationResult> validationErrors) : base(message)
        {
            ValidationErrors = validationErrors;
        }

        public List<ValidationResult> ValidationErrors { get; }
    }
}
