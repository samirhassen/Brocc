using nCustomer.DbModel;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;

namespace nCustomer.WebserviceMethods
{
    public class ContactInfoChangeValueMethod : ContactInfoMethodBase<ContactInfoChangeValueMethod.Request, ContactInfoChangeValueMethod.Response>
    {
        public override string Path => "ContactInfo/ChangeValue";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            if (!request.CustomerId.HasValue)
                return Error("Missing customerId", errorCode: "missingCustomerId");

            if (string.IsNullOrWhiteSpace(request.Name))
                return Error("Missing name", errorCode: "missingName");

            if (!CustomerUiModel.IsEditableContactInfoItem(request.Name))
                return Error($"{request.Name} is not editable", errorCode: "notEditable");

            var template = CustomerUiModel.GetTemplate(request.Name);
            var result = CustomerUiModel.ValidateAndNormalize(request.Value, template.UiType, NEnv.ClientCfgCore);
            if (!result.isValid)
                return Error($"Invalid value for '{request.Name}' of type {template.UiType.ToString()}", errorCode: "invalidValue");

            using (var db = new CustomersContext())
            {
                var repo = CreateRepo(requestContext, db);
                using (var tr = db.Database.BeginTransaction())
                {
                    repo.UpdateProperties(new List<CustomerPropertyModel>
                    {
                        new CustomerPropertyModel
                        {
                            CustomerId = request.CustomerId.Value,
                            Group = template.Group.ToString(),
                            IsSensitive = template.IsSensitive,
                            Name = request.Name,
                            Value = result.normalizedValue
                        }
                    }, true);
                    db.SaveChanges();
                    tr.Commit();
                }

                CustomerPropertyModelExtended currentValue = null;
                List<CustomerPropertyModelExtended> historicalValues = null;

                if (request.IncludesNewValuesInResponse.GetValueOrDefault())
                {
                    var currentAndHistoricalValues = repo.GetCurrentAndHistoricalValuesForProperty(request.CustomerId.Value, request.Name, requestContext.Service().User.GetUserDisplayNameByUserId);
                    currentValue = currentAndHistoricalValues.Item1;
                    historicalValues = currentAndHistoricalValues.Item2;
                }

                return new Response { currentValue = currentValue, historicalValues = historicalValues };
            }
        }

        public class Request
        {
            public int? CustomerId { get; set; }
            public string Name { get; set; }
            public string Value { get; set; }
            public bool? IncludesNewValuesInResponse { get; set; }
        }

        public class Response
        {
            public CustomerPropertyModelExtended currentValue { get; set; }
            public List<CustomerPropertyModelExtended> historicalValues { get; set; }
        }
    }
}