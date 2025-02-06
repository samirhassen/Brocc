using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods
{
    public class CreateWorkListReportMethod : FileStreamWebserviceMethod<CreateWorkListReportMethod.Request>
    {
        public override string Path => "WorkLists/CreateReport";

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var r = requestContext.Resolver();

            var s = r.Resolve<IWorkListService>();

            if (request.ReportName.EqualsIgnoreCase("InitialState"))
            {
                return ExcelFile(s.CreateWorkListInitialStateAsXlsx(request.WorkListId.Value), downloadFileName: $"WorkList-InitialState-{request.WorkListId.Value}.xlsx");
            }
            else if (request.ReportName.EqualsIgnoreCase("Result"))
            {
                var displayNameService = r.Resolve<IUserDisplayNameService>();
                return ExcelFile(s.CreateWorkListResultAsXlsx(request.WorkListId.Value, displayNameService.GetUserDisplayNameByUserId), downloadFileName: $"WorkList-Result-{request.WorkListId.Value}.xlsx");
            }
            else
                return Error("Invalid ReportName. Valid names are: InitialState, Result", errorCode: "invalidReportName");
        }

        public class Request
        {
            [Required]
            public int? WorkListId { get; set; }

            /// <summary>
            /// InitialState: Data initially in the list
            /// Result: Current completion state
            /// </summary>
            [Required]
            public string ReportName { get; set; }
        }
    }
}