using Autofac;
using NTech.Core.PreCredit.Shared.Services;

namespace nPreCredit.Code.Balanzia
{
    public static class BalanziaFiDiRegistration
    {
        public static void ConfigureServices(ContainerBuilder builder)
        {
            var isBalanziaFi = NEnv.ClientCfg.ClientName == "balanzia" || (!NEnv.IsProduction && NEnv.OptBool("ntech.forceservices.balanziafi"));

            if (!isBalanziaFi)
                return;

            R<LegacyUnsecuredCreditApplicationDbWriter, ILegacyUnsecuredCreditApplicationDbWriter>(builder);
            builder.RegisterType<UnsecuredCreditApplicationProviderRepository>().InstancePerRequest();
            builder.RegisterType<BalanziaFiUnsecuredLoanApplicationCreationService>().InstancePerRequest();
        }

        private static void R<TConcrete, TInterface>(ContainerBuilder builder) where TConcrete : TInterface
        {
            builder
                .RegisterType<TConcrete>()
                .As<TInterface>()
                .InstancePerRequest();
        }
    }
}