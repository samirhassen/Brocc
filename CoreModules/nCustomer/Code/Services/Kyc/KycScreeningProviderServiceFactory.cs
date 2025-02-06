using nCustomer.Code.Services.Aml.Cm1;
using nCustomer.Code.Services.Kyc.Cm1;
using nCustomer.Code.Services.Kyc.Mock;
using nCustomer.Code.Services.Kyc.Trapets;
using NTech.Services.Infrastructure;
using System;

namespace nCustomer.Code.Services.Kyc
{

    public class KycScreeningProviderServiceFactory : IKycScreeningProviderServiceFactory
    {
        public IKycScreeningProviderService CreateMultiCheckService()
        {
            var providerService = UseFirstActiveService(
                CreateTrapetsRestService,
                CreateCm1Service,
                CreateTrapetsSoapService);

            if (providerService != null)
                return providerService;

            if (NEnv.IsProduction)
                throw new Exception("Missing kyc screening provider");

            return new MultiMockKycScreeningProviderService();
        }

        public bool DoesCurrentProviderSupportContactInfo() => IsCm1Active(NEnv.Cm1Kyc) || NTechEnvironment.Instance.OptBoolSetting("ntech.customer.kyc.forcecontactinfo");

        private IKycScreeningProviderService CreateCm1Service()
        {
            var cm1 = NEnv.Cm1Kyc;
            if (!IsCm1Active(cm1))
                return null;
            return new CM1KycScreeningProviderService(NEnv.ClientCfg.Country.BaseCountry, CreateLogger(cm1.DebugLogFolder, "Cm1"), cm1, NEnv.IsProduction);
        }

        private IKycScreeningProviderService CreateTrapetsSoapService()
        {
            var settings = NEnv.TrapetsKycInstantWatchSoap;
            if (settings == null)
                return null;
            Tuple<string, string> alternateSingleQueryUsernameAndPassword = null;
            if (!string.IsNullOrWhiteSpace(settings.SpecialSingleQueryUsername) && !string.IsNullOrWhiteSpace(settings.SpecialSingleQueryPassword))
            {
                alternateSingleQueryUsernameAndPassword = Tuple.Create(settings.SpecialSingleQueryUsername, settings.SpecialSingleQueryPassword);
            }
            return new TrapetsSoapKycScreeningProviderService(settings.Username, settings.Password, settings.Endpoint, rawLog: CreateLogger(settings.DebugLogFolder, "Trapets"), skipSoundex: settings.SkipSoundex, alternateSingleQueryUsernameAndPassword: alternateSingleQueryUsernameAndPassword);
        }

        private IKycScreeningProviderService CreateTrapetsRestService()
        {
            var settings = NEnv.TrapetsKycInstantWatchRest;
            if (settings == null || settings.OptBool("isDisabled"))
                return null;
            return new TrapetsRestKycScreeningProviderService(settings, rawLog: CreateLogger(settings.Opt("debugLogFolder"), "TrapetsRest"));
        }

        private IKycScreeningProviderService UseFirstActiveService(params Func<IKycScreeningProviderService>[] factories)
        {
            foreach (var factory in factories)
            {
                var service = factory();
                if (service != null)
                    return service;
            }
            return null;
        }

        private bool IsCm1Active(Cm1KycSettings cm1Settings) => (cm1Settings != null && !cm1Settings.Disabled);

        private static Action<string> CreateLogger(string debugFolder, string context)
        {
            if (debugFolder == null)
                return null;

            return msg =>
            {
                System.IO.Directory.CreateDirectory(debugFolder);
                System.IO.File.WriteAllText(System.IO.Path.Combine(debugFolder, $"{context}-Result-" + Guid.NewGuid().ToString() + ".txt"), msg);
            };
        }
    }
}