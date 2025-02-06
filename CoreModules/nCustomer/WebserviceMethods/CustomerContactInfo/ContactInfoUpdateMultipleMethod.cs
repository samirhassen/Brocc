using nCustomer.DbModel;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;

namespace nCustomer.WebserviceMethods.CustomerContactInfo
{
    public class ContactInfoUpdateMultipleMethod : ContactInfoMethodBase<Request, Response>
    {
        public override string Path => "ContactInfo/UpdateMultiple";
        private readonly List<string> AllowedNamesToUpdate = new List<string>() { "email", "phone" };

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            if (!Validate(request, out var error))
                return error;

            var propertiesToUpdate = new List<CustomerPropertyModel>();

            foreach (var property in request.Properties)
            {
                var template = CustomerUiModel.GetTemplate(property.Name);
                var (isValid, normalizedValue) = CustomerUiModel.ValidateAndNormalize(property.Value, template.UiType, NEnv.ClientCfgCore);
                if (!isValid)
                    return Error($"Invalid value for '{property.Name}' of type {template.UiType.ToString()}", errorCode: "invalidValue");

                propertiesToUpdate.Add(new CustomerPropertyModel
                {
                    CustomerId = request.CustomerId.Value,
                    Group = template.Group.ToString(),
                    IsSensitive = template.IsSensitive,
                    Name = property.Name,
                    Value = normalizedValue
                });
            }

            using (var context = new CustomersContext())
            {
                var repo = CreateRepo(requestContext, context);
                using (var tr = context.Database.BeginTransaction())
                {
                    repo.UpdateProperties(propertiesToUpdate, true);
                    context.SaveChanges();
                    tr.Commit();
                }
            }

            return new Response();
        }

        private bool Validate(Request request, out Response error)
        {
            error = null;

            if (!request.CustomerId.HasValue)
                error = Error("Missing customerId", errorCode: "missingCustomerId");

            if (request.Properties == null)
                error = Error("Missing values to update", errorCode: "missingValues");

            foreach (var field in request.Properties)
            {
                if (!AllowedNamesToUpdate.Contains(field.Name.ToLowerInvariant()))
                    error = Error($"Not allowed to update {field.Name} in this method", errorCode: "notAllowedToEdit");
            }

            return error == null;
        }
    }

    public class Request
    {
        public int? CustomerId { get; set; }
        public List<NameValue> Properties { get; set; }

        public class NameValue
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }
    }

    public class Response
    {

    }
}