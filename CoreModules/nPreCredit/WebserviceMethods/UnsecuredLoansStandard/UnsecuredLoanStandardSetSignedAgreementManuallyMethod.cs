using Newtonsoft.Json;
using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class UnsecuredLoanStandardSetSignedAgreementManuallyMethod : TypedWebserviceMethod<UnsecuredLoanStandardSetSignedAgreementManuallyMethod.Request, UnsecuredLoanStandardSetSignedAgreementManuallyMethod.Response>
    {
        public override string Path => "UnsecuredLoanStandard/Agreement/Set-SignedAgreement-Manually";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled;
        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("Middle");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var agreementService = requestContext.Resolver().Resolve<UnsecuredLoanStandardAgreementService>();

            if (request.IsRemove ?? false)
            {
                agreementService.RemoveSignedAgreementDirectly(request.ApplicationNr);
            }
            else
            {
                agreementService.AttachSignedAgreementDirectly(request.ApplicationNr, request.DataUrl, request.Filename);
            }

            return new Response
            {

            };
        }

        public class Request : IValidatableObject
        {
            [Required]
            public string ApplicationNr { get; set; }

            public bool? IsRemove { get; set; }

            public string Filename { get; set; }

            public string DataUrl { get; set; }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                var request = (Request)validationContext.ObjectInstance;

                var isRemove = request.IsRemove ?? false;
                var isAttach = !string.IsNullOrWhiteSpace(request.Filename) && !string.IsNullOrWhiteSpace(request.Filename);

                if (isRemove == isAttach)
                    yield return new ValidationResult("Use exactly one of IsRemove or FileName+DataUrl");
            }
        }

        public class Response
        {

        }
    }
}