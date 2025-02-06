using NTech.Core.Module.Shared.Services;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods
{
    public class ValidateBankAccountNrBatchMethod : TypedWebserviceMethod<ValidateBankAccountNrBatchMethod.Request, ValidateBankAccountNrBulkResponse>
    {
        public override string Path => "bankaccount/validate-nr-batch";

        protected override ValidateBankAccountNrBulkResponse DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            if (request.AllowExternalSources == true)
                return Error("AllowExternalSources is not supported");

            var service = new BankAccountNrValidationService(NEnv.ClientCfgCore, _ => throw new NotImplementedException());
            return service.Validate(request);
        }

        public class Request : IValidatableObject, IValidateBankAccountNrBulkRequest<Request.Account>
        {
            [Required]
            public List<Account> Accounts { get; set; }

            public bool? AllowExternalSources { get; set; }

            public class Account : IValidateBankAccountNrBulkRequestAccount
            {
                public string BankAccountNr { get; set; }
                public string BankAccountNrType { get; set; }

                [Required]
                public string RequestKey { get; set; }
            }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                var request = (Request)validationContext.ObjectInstance;

                var count = request?.Accounts?.Count ?? 0;
                var keys = (request?.Accounts?.Select(x => x.RequestKey ?? "")?.ToHashSet() ?? new HashSet<string>());
                if (keys.Count != count)
                    yield return new ValidationResult("RequestKey must be different for every account since the result is keyed by it.");
            }
        }
    }
}