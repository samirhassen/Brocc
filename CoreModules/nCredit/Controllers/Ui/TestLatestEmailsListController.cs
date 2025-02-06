using NTech.Services.Infrastructure.Email;
using System.Linq;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [RoutePrefix("Ui/TestLatestEmails")]
    public class TestLatestEmailsListController : NController
    {
        [Route("List")]
        public ActionResult List()
        {
            SetInitialData(new
            {
                emails = InMemoryEmailTestService.GetStoredEmails().OrderByDescending(x => x.Date).ToList()
            });
            return View();
        }
    }
}