using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NTechSignicat.Models;
using NTechSignicat.Services;

namespace NTechSignicat.Controllers
{
    [Route("api")]
    [ApiController]
    public class DocumentApiController : Controller
    {
        private readonly IDocumentService documentService;

        public DocumentApiController(IDocumentService documentService)
        {
            this.documentService = documentService;
        }

        [Route("document/{documentKey}")]
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Document([FromRoute]string documentKey)
        {
            var d = this.documentService.Get(documentKey);
            if (d == null)
                return NotFound();
            return File(d.GetDocumentData(), d.DocumentMimeType, d.DocumentDownloadName, null, null, false);
        }

        [Route("document")]
        [HttpPost]
        public IActionResult DownloadDocument([FromBody]DownloadDocumentRequest request)
        {
            var d = this.documentService.Get(request.DocumentKey);
            return Json(d);
        }

        public class DownloadDocumentRequest
        {
            [Required]
            public string DocumentKey { get; set; }
        }
    }
}
