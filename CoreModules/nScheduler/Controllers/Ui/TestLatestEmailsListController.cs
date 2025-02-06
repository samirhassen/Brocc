using Newtonsoft.Json;
using NTech.Services.Infrastructure.Email;
using System;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace nScheduler.Controllers
{
    [RoutePrefix("Ui/TestLatestEmails")]
    public class TestLatestEmailsListController : NController
    {
        [Route("List")]
        public ActionResult List()
        {
            ViewBag.JsonInitialData = Convert.ToBase64String(Encoding.GetEncoding("iso-8859-1").GetBytes(JsonConvert.SerializeObject(new
            {
                emails = InMemoryEmailTestService.GetStoredEmails().OrderByDescending(x => x.Date).ToList()
            })));
            return View();
        }
    }
}