using nCustomer.DbModel;
using NTech.Banking.OrganisationNumbers;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nCustomer.WebserviceMethods.Company
{
    public class CreateOrUpdateCompanyCustomerMethod : TypedWebserviceMethod<CreateOrUpdateCompanyCustomerMethod.Request, CreateOrUpdateCompanyCustomerMethod.Response>
    {
        public override string Path => "CompanyCustomer/CreateOrUpdate";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            IOrganisationNumber nr;
            if (!NEnv.BaseOrganisationNumberParser.TryParse(request.Orgnr, out nr))
                return Error("Invalid orgnr", errorCode: "invalidOrgnr");

            var p = request.Properties?.ToDictionary(x => x.Name, x => x.Value) ?? new Dictionary<string, string>();
            var forceUpdateProperties = request?.Properties?.Where(x => x.ForceUpdate)?.Select(x => x.Name)?.ToHashSet() ?? new HashSet<string>();

            if (!string.IsNullOrWhiteSpace(request.CompanyName))
                p[CustomerProperty.Codes.companyName.ToString()] = request.CompanyName;

            var additionalSensitiveProperties = request.AdditionalSensitiveProperties == null ? null : new HashSet<string>(request.AdditionalSensitiveProperties);

            var customerId = requestContext.Service().CompanyCustomer.CreateOrUpdateCompany(
                nr,
                p,
                requestContext.CurrentUserMetadata(),
                additionalSensitiveProperties: additionalSensitiveProperties,
                forceUpdateProperties: forceUpdateProperties,
                expectedCustomerId: request.ExpectedCustomerId,
                externalEventCode: request.GetExternalEventCode());

            return new Response
            {
                CustomerId = customerId
            };
        }

        public class Request
        {
            [Required]
            public string Orgnr { get; set; }
            public string CompanyName { get; set; }
            public List<Property> Properties { get; set; }
            public List<string> AdditionalSensitiveProperties { get; set; }
            public int? ExpectedCustomerId { get; set; }
            public string EventType { get; set; }
            public string EventSourceId { get; set; }
            public class Property
            {
                public string Name { get; set; }
                public string Value { get; set; }
                public bool ForceUpdate { get; set; }
            }

            public string GetExternalEventCode()
            {
                if (string.IsNullOrWhiteSpace(EventType))
                    return null;
                return string.IsNullOrWhiteSpace(EventSourceId) ? EventType?.Trim() : $"{EventType}_{EventSourceId}";
            }
        }

        public class Response
        {
            public int CustomerId { get; set; }
        }
    }
}