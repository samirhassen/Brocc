using nCustomer.Code.Services.Aml.Cm1;
using Newtonsoft.Json;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using NTech.Legacy.Module.Shared;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.IO;
using System.Web.Mvc;

namespace nCustomer.Controllers
{
    [NTechAuthorize]
    public class AmlCm1Controller : NController
    {
        private Cm1AmlExportService CreateExportService()
        {
            var resolver = Service;
            var cm1Settings = new Lazy<NTechSimpleSettingsCore>(() => Cm1AmlExportService.GetCm1Settings(NTechEnvironmentLegacy.SharedInstance));
            var documentClient = LegacyServiceClientFactory.CreateDocumentClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry);
            return new Cm1AmlExportService(x => new CustomerWriteRepository(x, GetCurrentUserMetadata().CoreUser,
                CoreClock.SharedInstance, resolver.EncryptionService, NEnv.ClientCfgCore), cm1Settings, resolver.CustomerContextFactory,
                NEnv.ClientCfgCore, documentClient, NTechEnvironmentLegacy.SharedInstance);
        }

        [Route("Kyc/CreateCm1AmlExportFiles")]
        [HttpPost]
        public ActionResult CreateCm1AmlExportFiles()
        {
            PerProductCmlExportFileRequest request;
            Request.InputStream.Position = 0;
            using (var r = new StreamReader(Request.InputStream))
            {
                request = JsonConvert.DeserializeObject<PerProductCmlExportFileRequest>(r.ReadToEnd());
            }

            var service = CreateExportService();

            try
            {
                return Json2(service.CreateCm1AmlExportFilesAndUpdateCustomerExportStatus(request));
            }
            catch (NTechWebserviceMethodException ex)
            {
                if (ex.IsUserFacing)
                    return new HttpStatusCodeResult(ex.ErrorHttpStatusCode ?? 400, ex.Message);
                else
                    throw;
            }
        }

        [Route("Api/Kyc/SendUpdatesForAllCustomers")]
        [HttpPost]
        public ActionResult SendUpdatesForAllCustomers()
        {
            var service = CreateExportService();
            var errors = service.SendUpdatesForAllCustomersReturningErrors();
            return Json2(new { errors });
        }
    }
}