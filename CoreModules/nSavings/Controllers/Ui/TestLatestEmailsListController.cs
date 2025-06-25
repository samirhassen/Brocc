using System;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Newtonsoft.Json;
using NTech.Services.Infrastructure.Email;

namespace nSavings.Controllers.Ui
{
    [RoutePrefix("Ui/TestLatestEmails")]
    public class TestLatestEmailsListController : NController
    {
        [Route("List")]
        public ActionResult List()
        {
            ViewBag.JsonInitialData = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(new
                    {
                        emails = InMemoryEmailTestService.GetStoredEmails().OrderByDescending(x => x.Date).ToList()
                    })));
            return View();
        }
    }
}