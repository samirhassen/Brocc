using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using NTech.Shared;
using NTechSignicat.Models;
using NTechSignicat.Services;

namespace NTechSignicat.Controllers
{
    public class CommonController : Controller
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<CommonController> logger;
        private readonly INEnv nEnv;
        private readonly IDocumentDatabaseService databaseService;

        public CommonController(IConfiguration configuration, ILogger<CommonController> logger, INEnv nEnv, IDocumentDatabaseService documentDatabaseService)
        {
            this.configuration = configuration;
            this.logger = logger;
            this.nEnv = nEnv;
            databaseService = documentDatabaseService;
        }

        [AllowAnonymous]
        [Route("hb")]
        public IActionResult Hb()
        {
            var a = System.Reflection.Assembly.GetExecutingAssembly();
            var clientResourceFile = nEnv.ClientResourceFile("releaseNumber", "CurrentReleaseMetadata.txt", mustExist: false);
            return Json(new
            {
                status = "ok",
                name = a.GetName().Name,
                build = System.Reflection.AssemblyName.GetAssemblyName(a.Location).Version.ToString(),
                isProduction = this.configuration.GetValue<string>("ntech.isproduction"),
                release = NTechSimpleSettings.GetValueFromClientResourceFile(clientResourceFile, "releaseNumber", "No Current Release Info")
            });
        }

        /// <summary>
        /// Follows same pattern as other modules.
        /// Called using generic parameters from StagingDatabaseTransformer.SetupServicesAfterRestore. 
        /// </summary>
        /// <returns></returns>
        [Route("Setup")]
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Setup(bool? verifyDb = null, bool? clearCache = null)
        {
            if (nEnv.IsProduction)
            {
                throw new InvalidOperationException("Cannot call Setup in production environment. ");
            }

            var success = databaseService.DeleteAll();

            return Json(new { isDbRestored = success });
        }

    }
}
