using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace nTest.Controllers
{
    [RoutePrefix("Ui/ApiDocumentation")]
    public class ApiDocumentationController : NController
    {
        [Route("")]
        public ActionResult Index()
        {
            var navigationTargetToHere = NTechNavigationTarget.CreateCrossModuleNavigationTargetCode("TestModuleApiDocumentation", null);

            var servicesNamesAndUri = new List<Tuple<string, Uri>>();
            var r = NEnv.ServiceRegistry;
            Action<string> addIfExists = n =>
                {
                    if (r.ContainsService(n))
                        servicesNamesAndUri.Add(
                            Tuple.Create(
                                n,
                                r.External.ServiceUrl(n, "Api/Docs",
                                    Tuple.Create("backTarget", navigationTargetToHere))));
                };
            addIfExists("nPreCredit");
            addIfExists("nCredit");
            addIfExists("nSavings");
            addIfExists("nCustomer");
            addIfExists("nDataWarehouse");

            return View(new IndexModel
            {
                Services = servicesNamesAndUri.Select(x => new IndexModel.Service
                {
                    Name = x.Item1,
                    DocumentationUrl = x.Item2.ToString()
                }).ToList()
            });
        }

        public class IndexModel
        {
            public class Service
            {
                public string Name { get; set; }
                public string DocumentationUrl { get; set; }
            }
            public List<Service> Services { get; set; }
        }
    }
}