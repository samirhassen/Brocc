using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods
{
    public class EditApplicationDataBatchedMethod : TypedWebserviceMethod<EditApplicationDataBatchedMethod.Request, EditApplicationDataBatchedMethod.Response>
    {
        public override string Path => "Application/Edit/SetItemDataBatched";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            foreach (var e in request.Edits)
            {
                if (!e.IsDelete.Value && string.IsNullOrWhiteSpace(e.NewValue))
                    return Error("NewValue is required when IsDelete != true", httpStatusCode: 400, errorCode: "newValueRequired");
            }

            var i = requestContext.Resolver().Resolve<ApplicationDataSourceService>();

            var wasEdited = i.SetDataBatch(request.ApplicationNr, request.Edits.Select(x => new ApplicationDataSourceEditModel
            {
                CompoundItemName = x.ItemName,
                DataSourceName = x.DataSourceName,
                IsDelete = x.IsDelete.Value,
                NewValue = x.NewValue
            }).ToList(), requestContext.CurrentUserMetadata());

            return new Response
            {
                WasEdited = wasEdited
            };
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }

            public class EditModel
            {
                [Required]
                public string DataSourceName { get; set; }

                [Required]
                public string ItemName { get; set; }

                [Required]
                public bool? IsDelete { get; set; }

                public string NewValue { get; set; }
            }

            [Required]
            public List<EditModel> Edits { get; set; }
        }

        public class Response
        {
            public bool WasEdited { get; set; }
        }
    }
}