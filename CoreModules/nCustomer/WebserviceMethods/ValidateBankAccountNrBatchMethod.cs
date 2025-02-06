using nCustomer.Code.Services;
using NTech.Core.Module.Shared.Services;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nCustomer.WebserviceMethods
{
    public class ValidateBankAccountNrBatchMethod : TypedWebserviceMethod<ValidateBankAccountNrBatchMethod.Request, ValidateBankAccountNrBulkResponse>
    {
        public override string Path => "bankaccount/validate-nr-batch";

        protected override ValidateBankAccountNrBulkResponse DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var externalDataService = new ExternalBankAccountValidationService(new ServiceClientSyncConverterLegacy());
            var service = new BankAccountNrValidationService(NEnv.ClientCfgCore, externalDataService.GetExternalData);
            return service.Validate(request);
        }

        public class Request : IValidatableObject, IValidateBankAccountNrBulkRequest<Request.Account>
        {
            [Required]
            public List<Account> Accounts { get; set; }

            /// <summary>
            /// Allows things like contacting bgc to aquire the owner of a bg account
            /// </summary>
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