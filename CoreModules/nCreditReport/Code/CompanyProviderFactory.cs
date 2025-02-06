using nCreditReport.Code.UcSe;
using System;
using System.Collections.Generic;

namespace nCreditReport.Code
{
    public static class CompanyProviderFactory
    {
        public const string UcBusinessSeProviderName = "UcBusinessSe";

        private static Lazy<Dictionary<string, Func<CompanyBaseCreditReportService>>> serviceFactoryByName = new Lazy<Dictionary<string, Func<CompanyBaseCreditReportService>>>(() =>
        {
            return new Dictionary<string, Func<CompanyBaseCreditReportService>>(StringComparer.InvariantCultureIgnoreCase)
            {
                {   UcBusinessSeProviderName,
                    () =>
                    {
                        var s = NEnv.UcBusinessSe;
                        if(NEnv.IsProduction)
                            return new UcBusinessSeService(UcBusinessSeProviderName, s);
                        else
                        {
                            if(s.TestReplacementOrgnr != null && !string.IsNullOrWhiteSpace(s.TestModuleMode) && s.TestModuleMode != "notused")
                                throw new Exception("UcBusiness. TestReplacementOrgnr and TestModuleMode != notused cannot be combined");

                            if(s.TestReplacementOrgnr != null)
                                return new OrgNrSwapCreditReportService(new UcBusinessSeService(UcBusinessSeProviderName, s), x => Tuple.Create(s.TestReplacementOrgnr, new Dictionary<string, string>()));
                            else
                                return new TestAwareUcBusinessSeService(UcBusinessSeProviderName, s);
                        }
                    }
                }
            };
        });

        public static bool Exists(string providerName)
        {
            return serviceFactoryByName.Value.ContainsKey(providerName);
        }

        public static CompanyBaseCreditReportService Create(string providerName, Dictionary<string, string> additionalParameters)
        {
            return serviceFactoryByName.Value[providerName]();
        }
    }
}