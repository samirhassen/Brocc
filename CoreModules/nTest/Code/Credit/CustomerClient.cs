using NTech.Banking.CivicRegNumbers;
using NTech.Banking.OrganisationNumbers;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;

namespace nTest.Code.Credit
{
    public class CustomerClient
    {
        private NHttp.NHttpCall Begin()
        {
            return NHttp.Begin(NEnv.ServiceRegistry.Internal.ServiceRootUri("nCustomer"), NEnv.AutomationBearerToken());
        }

        public CreateOrUpdateCompanyResponse CreateOrUpdateCompany(CreateOrUpdateCompanyRequest request)
        {
            return Begin()
                .PostJson("Api/CompanyCustomer/CreateOrUpdate", request)
                .ParseJsonAs<CreateOrUpdateCompanyResponse>();
        }

        public class CreateOrUpdateCompanyRequest
        {
            public string Orgnr { get; set; }
            public string CompanyName { get; set; }
            public List<Property> Properties { get; set; }
            public List<string> AdditionalSensitiveProperties { get; set; }
            public string EventType { get; set; }
            public string EventSourceId { get; set; }
            public int? ExpectedCustomerId { get; set; }

            public class Property
            {
                public string Name { get; set; }
                public string Value { get; set; }
            }
        }

        public class CreateOrUpdateCompanyResponse
        {
            public int CustomerId { get; set; }
        }

        public int GetCustomerId(ICivicRegNumber civicRegNr)
        {
            return Begin()
                .PostJson("api/CustomerIdByCivicRegNr", new
                {
                    civicRegNr = civicRegNr.NormalizedValue,
                })
                .ParseJsonAs<GetCustomerIdResult>()
                .CustomerId;
        }

        public int GetCustomerId(IOrganisationNumber orgnr)
        {
            return Begin()
                .PostJson("api/CustomerIdByOrgnr", new
                {
                    orgnr = orgnr.NormalizedValue,
                })
                .ParseJsonAs<GetCustomerIdResult>()
                .CustomerId;
        }

        public void SetLocalKycDecision(int customerId, bool isModellingPep, bool currentValue)
        {
            Begin()
                .PostJson("Api/KycManagement/SetLocalDecision", new { customerId, isModellingPep, currentValue, includeNewCurrentData = false })
                .EnsureSuccessStatusCode();
        }

        private class GetCustomerIdResult
        {
            public int CustomerId { get; set; }
        }


        public CreateOrUpdatePersonResponse CreateOrUpdatePerson(CreateOrUpdatePersonRequest request)
        {
            return Begin()
                .PostJson("Api/PersonCustomer/CreateOrUpdate", request)
                .ParseJsonAs<CreateOrUpdatePersonResponse>();
        }

        public class CreateOrUpdatePersonRequest
        {
            public string CivicRegNr { get; set; }
            public DateTime? BirthDate { get; set; }
            public List<Property> Properties { get; set; }
            public List<string> AdditionalSensitiveProperties { get; set; }
            public string EventType { get; set; }
            public string EventSourceId { get; set; }
            public int? ExpectedCustomerId { get; set; }

            public class Property
            {
                public string Name { get; set; }
                public string Value { get; set; }
                public bool ForceUpdate { get; set; }
            }
        }

        public class CreateOrUpdatePersonResponse
        {
            public int CustomerId { get; set; }
        }
    }
}