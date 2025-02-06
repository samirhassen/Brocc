using nCustomer.DbModel;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nCustomer.WebserviceMethods.Company
{
    public class CreateOrUpdatePersonCustomerMethod : TypedWebserviceMethod<CreateOrUpdatePersonCustomerMethod.Request, CreateOrUpdatePersonCustomerMethod.Response>
    {
        public override string Path => "PersonCustomer/CreateOrUpdate";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            if (!NEnv.BaseCivicRegNumberParser.TryParse(request.CivicRegNr, out var civicNr))
                return Error("Invalid civicRegNr", errorCode: "invalidCivicRegNr");

            var propertiesList = request.Properties?.ToDictionary(x => x.Name, x => x.Value) ?? new Dictionary<string, string>();
            var forceUpdateProperties = request?.Properties?.Where(x => x.ForceUpdate)?.Select(x => x.Name)?.ToHashSet() ?? new HashSet<string>();

            var birthDate = request.BirthDate ?? civicNr.BirthDate;

            if (birthDate.HasValue)
                propertiesList[CustomerProperty.Codes.birthDate.ToString()] = birthDate.Value.ToString("yyyy-MM-dd");

            var additionalSensitiveProperties = request.AdditionalSensitiveProperties == null ? null : new HashSet<string>(request.AdditionalSensitiveProperties);

            var customerId = requestContext.Service().PersonCustomer.CreateOrUpdatePerson(
                civicNr,
                propertiesList,
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
            public string CivicRegNr { get; set; }
            public DateTime? BirthDate { get; set; }
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