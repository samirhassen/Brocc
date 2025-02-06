using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace nSavings.Controllers
{
    [NTechApi]
    [RoutePrefix("Api/Documents")]
    public class ApiDocumentsController : NController
    {
        [HttpPost]
        [Route("DocumentsInitialData")]
        public ActionResult DocumentsInitialData(string savingsAccountNr)
        {
            var documents = new List<DocumentModel>();

            foreach (var year in Service.YearlySummary.GetAllYearsWithSummaries(savingsAccountNr))
            {
                documents.Add(new DocumentModel
                {
                    CreationDate = new DateTime(year + 1, 1, 1).AddDays(-1),
                    DocumentData = year.ToString(),
                    DocumentType = "YearlySummary",
                    DownloadUrl = Url.Action("ShowPdf", "ApiYearlySummaries", new { savingsAccountNr, year, fileDownloadName = $"YearlySummary_{savingsAccountNr}_{year}.pdf" })
                });
            }

            return Json2(new
            {
                documents = documents
            });
        }

        private class DocumentModel
        {
            public string DocumentType { get; set; }
            public string DocumentData { get; set; }
            public DateTime CreationDate { get; set; }
            public string DownloadUrl { get; set; }
        }
    }
}