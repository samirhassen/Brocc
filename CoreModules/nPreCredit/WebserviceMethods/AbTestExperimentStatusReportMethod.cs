using Dapper;
using nPreCredit.Code;
using nPreCredit.Code.Services;
using nPreCredit.DbModel;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Configuration;

namespace nPreCredit.WebserviceMethods
{
    public class AbTestExperimentStatusReportMethod : FileStreamWebserviceMethod<AbTestExperimentStatusReportMethod.Request>
    {
        public override string Path => "AbTesting/Reports/ExperimentStatus";

        public override bool IsEnabled => NEnv.IsUnsecuredLoansEnabled || NEnv.IsMortgageLoansEnabled;

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var sheets = new List<DocumentClientExcelRequest.Sheet>();

            sheets.Add(new DocumentClientExcelRequest.Sheet
            {
                AutoSizeColumns = true,
                Title = "Applications"
            });
            sheets.Add(new DocumentClientExcelRequest.Sheet
            {
                AutoSizeColumns = true,
                Title = "Overview"
            });

            AbTestingExperiment e;
            using (var context = requestContext.Resolver().Resolve<PreCreditContextFactoryService>().Create())
            {
                e = context.AbTestingExperiments.SingleOrDefault(x => x.Id == request.ExperimentId.Value);
                if (e == null)
                    return Error("No such experiment exists", httpStatusCode: 400, errorCode: "noSuchExperimentExists");
            }

            var applications = GetApplicationsInExperiment(e.Id);

            sheets[0].SetColumnsAndData(applications,
                applications.Col(x => x.ApplicationNr, ExcelType.Text, "ApplicationNr"),
                applications.Col(x => x.HasVariation ? 0 : 1, ExcelType.Number, "Group A", includeSum: true, nrOfDecimals: 0),
                applications.Col(x => x.HasVariation ? 1 : 0, ExcelType.Number, "Group B", includeSum: true, nrOfDecimals: 0),
                applications.Col(x => x.VariationName, ExcelType.Text, "VariationName"));

            var overviews = new List<(string Label, string Value)>();
            Action<string, string> add = (x, y) => overviews.Add((x, y));

            add("Experiment name", e.ExperimentName);
            add("Experiment id", e.Id.ToString());
            add("Start Date", e.StartDate?.ToString("yyyy-MM-dd") ?? "-");
            add("End Date", e.EndDate?.ToString("yyyy-MM-dd") ?? "-");
            add("Is Active", e.IsActive ? "yes" : "no");
            add("Max Count", e.MaxCount.HasValue ? e.MaxCount.Value.ToString() : "-");
            add("Current count", applications.Count.ToString());
            add("Desired Percent B", e.VariationPercent.HasValue ? e.VariationPercent?.ToString("F2") + "%" : "-");

            var f = applications.Count == 0 ? 0m : 100m * ((decimal)applications.Count(x => x.HasVariation)) / ((decimal)applications.Count);
            add("Current Percent B", f.ToString("F2") + "%");

            sheets[1].SetColumnsAndData(overviews,
                overviews.Col(x => x.Label, ExcelType.Text, "Label"),
                overviews.Col(x => x.Value, ExcelType.Text, "Value"));

            var excelRequest = new DocumentClientExcelRequest
            {
                Sheets = sheets.ToArray()
            };

            var client = new nDocumentClient();
            var result = client.CreateXlsx(excelRequest);

            return File(result, downloadFileName: $"AbTestingExperimentStatus-{e.Id}.xlsx");
        }

        private List<TempApplication> GetApplicationsInExperiment(int experimentId)
        {
            using (var connection = new SqlConnection(WebConfigurationManager.ConnectionStrings["PreCreditContext"].ConnectionString))
            {
                connection.Open();

                return connection.Query<TempApplication>(@"with ExperimentItems
as
(
	select	c.* 
	from	ComplexApplicationListItem c 
	where	c.ListName = 'AbTestingExperiment' 
	and		c.Nr = 1 
	and		c.IsRepeatable = 0
),
ApplicationNrs
as
(
	select	distinct i.ApplicationNr
	from	ExperimentItems i
	where	i.ItemName = 'ExperimentId'
	and		i.ItemValue = @experimentId
)
select	a.ApplicationNr,
		(select top 1 i.ItemValue from ExperimentItems i where i.ItemName = 'HasVariation' and i.ApplicationNr = a.ApplicationNr) as HasVariation,
		(select top 1 i.ItemValue from ExperimentItems i where i.ItemName = 'VariationName' and i.ApplicationNr = a.ApplicationNr) as VariationName
from	ApplicationNrs a", param: new { experimentId = experimentId.ToString() })
                    .ToList();
            }
        }

        public class Request
        {
            [Required]
            public int? ExperimentId { get; set; }
        }

        //Just to enable batching
        private class TempApplication
        {
            public string ApplicationNr { get; internal set; }
            public bool HasVariation { get; internal set; }
            public string VariationName { get; internal set; }
        }
    }
}