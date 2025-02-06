using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods
{
    public class EditApplicationDataMethod : TypedWebserviceMethod<EditApplicationDataMethod.Request, EditApplicationDataMethod.Response>
    {
        public override string Path => "Application/Edit/SetItemData";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            if (!request.IsDelete.Value && string.IsNullOrWhiteSpace(request.NewValue))
                return Error("NewValue is required when IsDelete != true", httpStatusCode: 400, errorCode: "newValueRequired");

            var i = requestContext.Resolver().Resolve<ApplicationDataSourceService>();

            var changeId = i.SetData(request.ApplicationNr, new ApplicationDataSourceEditModel
            {
                CompoundItemName = request.ItemName,
                DataSourceName = request.DataSourceName,
                IsDelete = request.IsDelete.Value,
                NewValue = request.NewValue
            }, requestContext.CurrentUserMetadata());

            return new Response
            {
                ChangeId = changeId
            };
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }

            [Required]
            public string DataSourceName { get; set; }

            [Required]
            public string ItemName { get; set; }

            [Required]
            public bool? IsDelete { get; set; }

            public string NewValue { get; set; }
        }

        public class Response
        {
            public int? ChangeId { get; set; }
        }
    }
}