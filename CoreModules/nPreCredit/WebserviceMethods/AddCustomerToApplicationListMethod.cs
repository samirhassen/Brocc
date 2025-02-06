using nPreCredit.Code.Services;
using NTech.Core.Module.Shared.Clients;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods
{
    public class AddCustomerToApplicationListMethod : TypedWebserviceMethod<AddCustomerToApplicationListMethod.Request, AddCustomerToApplicationListMethod.Response>
    {
        public override string Path => "ApplicationCustomerList/Add-Customer";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var resolver = requestContext.Resolver();

            var cc = resolver.Resolve<ICustomerClient>();

            if (request.CreateOrUpdateData != null)
            {
                var u = request.CreateOrUpdateData;
                var r = new CreateOrUpdatePersonRequest
                {
                    CivicRegNr = u.CivicRegNr,
                    ExpectedCustomerId = request.CustomerId.Value,
                    EventSourceId = request.ApplicationNr,
                    EventType = "AddCustomerToApplicationList_" + request.ListName,
                    Properties = new List<CreateOrUpdatePersonRequest.Property>()
                };
                Action<string, string> add = (name, value) =>
                {
                    if (string.IsNullOrWhiteSpace(value)) return;
                    r.Properties.Add(new CreateOrUpdatePersonRequest.Property { Name = name, Value = value });
                };

                add("firstName", u.FirstName);
                add("lastName", u.LastName);
                add("email", u.Email);
                add("phone", u.Phone);
                add("addressStreet", u.AddressStreet);
                add("addressZipcode", u.AddressZipcode);
                add("addressCity", u.AddressCity);
                add("addressCountry", u.AddressCountry);

                cc.CreateOrUpdatePerson(r);
            }

            bool wasAdded = false;
            resolver.Resolve<CreditApplicationCustomerListService>().SetMemberStatus(
                request.ListName, true, request.CustomerId.Value, applicationNr: request.ApplicationNr, observeStatusChange: x => wasAdded = x);

            return new Response { CustomerId = request.CustomerId.Value, WasAdded = wasAdded };
        }

        public class Response
        {
            public int CustomerId { get; set; }
            public bool WasAdded { get; set; }
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
            [Required]
            public string ListName { get; set; }
            [Required]
            public int? CustomerId { get; set; }
            public CustomerData CreateOrUpdateData { get; set; }

            public class CustomerData
            {
                [Required]
                public string CivicRegNr { get; set; }
                public string FirstName { get; set; }
                public string LastName { get; set; }
                public string Email { get; set; }
                public string Phone { get; set; }
                public string AddressStreet { get; set; }
                public string AddressZipcode { get; set; }
                public string AddressCity { get; set; }
                public string AddressCountry { get; set; }
            }
        }
    }
}