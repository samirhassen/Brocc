using nCustomer.DbModel;
using Newtonsoft.Json;
using NTech.Core.Customer.Shared.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.Linq;

namespace nCustomer.WebserviceMethods
{
    public class ContactInfoFetchEditValueDataMethod : ContactInfoMethodBase<ContactInfoFetchEditValueDataMethod.Request, ContactInfoFetchEditValueDataMethod.Response>
    {
        public override string Path => "ContactInfo/FetchEditValueData";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            if (!request.CustomerId.HasValue)
                return Error("Missing customerId", errorCode: "missingCustomerId");

            if (string.IsNullOrWhiteSpace(request.Name))
                return Error("Missing name", errorCode: "missingName");

            if (!CustomerUiModel.IsEditableContactInfoItem(request.Name))
                return Error($"{request.Name} is not editable", errorCode: "notEditable");

            var template = CustomerUiModel.GetTemplate(request.Name);

            using (var db = new CustomersContext())
            {
                var repo = CreateRepo(requestContext, db);

                var currentAndHistoricalValues = repo.GetCurrentAndHistoricalValuesForProperty(request.CustomerId.Value, request.Name, requestContext.Service().User.GetUserDisplayNameByUserId);

                var currentValue = currentAndHistoricalValues.Item1;
                var historicalValues = currentAndHistoricalValues.Item2;

                var response =  new Response
                {
                    customerId = request.CustomerId,
                    name = request.Name,
                    templateName = template.UiType.ToString(),
                    currentValue = currentValue,
                    historicalValues = historicalValues
                };

                if(request.IncludeTranslationTable == true)
                {
                    var translationService = new UiTranslationService(() => NEnv.IsTranslationCacheDisabled, NEnv.ClientCfgCore);
                    response.translationTableJson = JsonConvert.SerializeObject(translationService.GetTranslationTable());
                }

                return response;
            }
        }

        public class Response
        {
            public int? customerId { get; set; }
            public string name { get; set; }
            public string templateName { get; set; }
            public CustomerPropertyModelExtended currentValue { get; set; }
            public List<CustomerPropertyModelExtended> historicalValues { get; set; }
            public string translationTableJson { get; set; }
        }

        public class Request
        {
            public int? CustomerId { get; set; }
            public string Name { get; set; }
            public bool? IncludeTranslationTable { get; set; }
        }
    }
}