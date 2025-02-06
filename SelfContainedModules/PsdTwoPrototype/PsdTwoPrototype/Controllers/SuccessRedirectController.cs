using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
namespace PsdTwoPrototype.Controllers
{
    [Route("api/")]
    public class SuccessRedirectController : Controller
    {
        [HttpGet]
        [Route("SuccessRedirect/{internalRequestId}")]
        public IActionResult Get()
        {
            string internalRequestId = (string)this.RouteData.Values["internalRequestId"];
            var stream = new FileStream("PDFs/" + internalRequestId + ".pdf", FileMode.Open);

            System.Net.Mime.ContentDisposition cd = new System.Net.Mime.ContentDisposition
            {
                FileName = internalRequestId + "_pdf.pdf",
                Inline = true  //true = browser to try to show the file inline
            };

            Response.Headers.Add("Content-Disposition", cd.ToString());
            return File(stream, "application/pdf", internalRequestId + "_pdf.pdf");
        }


    }
}


