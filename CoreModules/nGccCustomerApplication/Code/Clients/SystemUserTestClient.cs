using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace nGccCustomerApplication.Code.Clients
{
    public class SystemUserTestClient : AbstractSystemUserServiceClient
    {
        protected override string ServiceName => "nTest";

        public List<Dictionary<string, string>> GenerateTestPersons(bool? isAccepted = null, int? seed = null, int? count = null, List<string> newPersonCustomizations = null)
        {
            var applicants = Begin(timeout: TimeSpan.FromMinutes(1))
                .PostJson("Api/TestPerson/Generate", new { isAccepted, seed, count, newPersonCustomizations })
                .ParseJsonAsAnonymousType(new { applicants = (List<string>)null })?.applicants;
            return applicants
                ?.Select(x => JsonConvert.DeserializeObject<Dictionary<string, string>>(x))
                ?.ToList();
        }
    }
}