using nCustomer.DbModel;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace nCustomer.WebserviceMethods
{
    public class ContactInfoFetchMethod : ContactInfoMethodBase<ContactInfoFetchMethod.Request, ContactInfoFetchMethod.Response>
    {
        public override string Path => "ContactInfo/Fetch";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var customerId = request.CustomerId.Value;
            var includeCivicRegNr = request.IncludeCivicRegNr.GetValueOrDefault();
            var includeSensitive = request.IncludeSensitive.GetValueOrDefault();

            using (var db = new CustomersContext())
            {
                var repo = CreateRepo(requestContext, db);

                var itemsNamesToFetch = new List<string>() { "firstName", "birthDate", "email", "phone", "isCompany", "companyName" };
                var sensitiveItems = new List<string>() { "addressStreet", "lastName", "addressZipcode", "addressCity", "addressCountry" };

                if (includeCivicRegNr)
                {
                    sensitiveItems.Add("civicRegNr");
                    sensitiveItems.Add("orgnr");
                }

                if (includeSensitive)
                    itemsNamesToFetch.AddRange(sensitiveItems);

                var props = repo.GetProperties(customerId, onlyTheseNames: itemsNamesToFetch, skipDecryptingEncryptedItems: false).ToDictionary(x => x.Name, x => x.Value);

                Func<string, DateTime?> parseDate = s => string.IsNullOrWhiteSpace(s) ? null : new DateTime?(DateTime.ParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None));

                var response = new Response
                {
                    customerId = customerId,

                    firstName = props.Opt("firstName"),
                    lastName = props.Opt("lastName"),
                    birthDate = parseDate(props.Opt("birthDate")),
                    civicRegNr = props.Opt("civicRegNr"),

                    orgnr = props.Opt("orgnr"),
                    isCompany = props.Opt("isCompany"),
                    companyName = props.Opt("companyName"),

                    addressStreet = props.Opt("addressStreet"),
                    addressZipcode = props.Opt("addressZipcode"),
                    addressCity = props.Opt("addressCity"),
                    addressCountry = props.Opt("addressCountry"),

                    email = props.Opt("email"),
                    phone = props.Opt("phone"),

                    sensitiveItems = sensitiveItems,
                    
                    includeSensitive = includeSensitive,
                    includeCivicRegNr = includeCivicRegNr
                };

                response.isCompanyBool = response.isCompany == "true";

                return response;
            }
        }

        public class Request
        {
            [Required]
            public int? CustomerId { get; set; }

            public bool? IncludeSensitive { get; set; }
            public bool? IncludeCivicRegNr { get; set; }
        }

        public class Response
        {
            public int customerId { get; set; }
            //NOTE: Lowercase here since we want it to map exactly to the model names in the lists and in the db and in javascript. Not ideal but the bugs from mismatches seem worse.

            public string firstName { get; set; }
            public string lastName { get; set; }
            public DateTime? birthDate { get; set; }
            public string civicRegNr { get; set; }
            public string orgnr { get; set; }
            public string isCompany { get; set; }
            public string companyName { get; set; }
            public string addressStreet { get; set; }
            public string addressZipcode { get; set; }
            public string addressCity { get; set; }
            public string addressCountry { get; set; }
            public string email { get; set; }
            public string phone { get; set; }
            public List<string> sensitiveItems { get; set; }

            public bool includeSensitive { get; set; }
            public bool includeCivicRegNr { get; set; }

            //Wierd name for legacy reasons to preserve the strange isCompany string response for callers that depend on it
            public bool isCompanyBool { get; set; }
        }
    }
}