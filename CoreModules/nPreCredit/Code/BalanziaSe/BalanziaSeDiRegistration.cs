using Autofac;
using NTech.Banking.ScoringEngine;

namespace nPreCredit.Code.BalanziaSe
{
    public static class BalanziaSeDiRegistration
    {
        public static void ConfigureServices(ContainerBuilder builder)
        {
            var isBalanziaSe = NEnv.ClientCfg.ClientName == "balanziaSe" || (!NEnv.IsProduction && NEnv.OptBool("ntech.forceservices.balanziase"));

            if (!isBalanziaSe)
                return;

            R<BalanziaSeScoringProcessDataSource, IPluginScoringProcessDataSource>(builder);
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