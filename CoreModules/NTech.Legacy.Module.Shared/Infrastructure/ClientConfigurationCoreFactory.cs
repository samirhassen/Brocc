using NTech.Core.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using System;
using System.IO;
using System.Xml.Linq;

namespace NTech.Legacy.Module.Shared.Infrastructure
{
    public static class ClientConfigurationCoreFactory
    {
        public static ClientConfigurationCore CreateUsingNTechEnvironment(NTechEnvironment environment)
        {
            var fn = environment.Setting("ntech.clientcfgfile", false);
            if (fn == null)
            {
                var f = environment.Setting("ntech.clientresourcefolder", false);
                if (f == null)
                    throw new Exception("Missing appsetting 'ntech.clientcfgfile'");
                else
                    fn = Path.Combine(f, "ClientConfiguration.xml");
            }

            return ClientConfigurationCore.CreateUsingXDocument(XDocument.Load(fn));
        }
    }
}
