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
    public class MockAuthenticationController : Controller
    {
        private readonly SignicatSettings signicatSettings;

        public MockAuthenticationController(SignicatSettings signicatSettings)
        {
            this.signicatSettings = signicatSettings;
        }

        [AllowAnonymous]
        [Route("mock-authenticate")]
        [HttpGet]
        public IActionResult Authenticate([FromQuery] string sessionId)
        {
            if (!signicatSettings.UseLocalMockForLogin)
                return NotFound();

            ViewBag.SessionId = sessionId;

            return View();
        }

        [AllowAnonymous]
        [Route("mock-authenticate-post")]
        [HttpPost]
        public IActionResult PostAuthenticate([FromForm] string sessionId)
        {
            if (!signicatSettings.UseLocalMockForLogin)
                return NotFound();

            return RedirectToAction("Callback", "SignicatAuthenticationCallback", new SignicatAuthenticationCallbackController.AuthenticationRedirectResultModel
            {
                state = sessionId,
                code = "mock"
            });
        }
    }
}