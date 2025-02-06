using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NTech.Banking.CivicRegNumbers;
using NTechSignicat.Models;
using NTechSignicat.Services;

namespace NTechSignicat.Controllers
{
    public class MockSignatureController : Controller
    {
        private readonly SignicatSettings signicatSettings;

        public MockSignatureController(SignicatSettings signicatSettings)
        {
            this.signicatSettings = signicatSettings;
        }

        [AllowAnonymous]
        [Route("mock-sign")]
        [HttpGet]
        public IActionResult Sign([FromQuery] string request_id, [FromQuery] string taskId)
        {
            if (!signicatSettings.UseLocalMockForSignatures)
                return NotFound();

            ViewBag.RequestId = request_id;
            ViewBag.TaskId = taskId;

            return View();
        }

        [AllowAnonymous]
        [Route("mock-sign-post")]
        [HttpPost]
        public IActionResult PostSign([FromForm] MockSignPostBodyModel body)
        {
            if (!signicatSettings.UseLocalMockForSignatures)
                return NotFound();

            return RedirectToAction("Callback", "SignicatSignatureCallback", new
            {
                request_id = body.RequestId,
                taskId = body.TaskId,
                status = "taskcomplete"
            });
        }

        public class MockSignPostBodyModel
        {
            public string RequestId { get; set; }
            public string TaskId { get; set; }
        }
    }
}