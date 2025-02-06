using nCreditReport.Code.BisnodeFi;
using nCreditReport.Code.CreditSafeSe;
using nCreditReport.Code.SatFi;
using nCreditReport.Code.TestOnly;
using nCreditReport.Code.UcSe;
using System;
using System.Collections.Generic;

namespace nCreditReport.Code
{
    public static class PersonProviderFactory
    {
        private static Lazy<Dictionary<string, Func<PersonBaseCreditReportService>>> serviceFactoryByName = new Lazy<Dictionary<string, Func<PersonBaseCreditReportService>>>(() =>
        {
            PersonBaseCreditReportService CreateServiceWithTestSupport(Func<PersonBaseCreditReportService> createProductionService, Func<ICreditReportCommonTestSettings> getTestSettings)
            {
                if (NEnv.IsProduction)
                    return createProductionService();
                else
                {
                    return new TestAwarePersonCreditReportService(createProductionService(), getTestSettings());
                }
            }

            return new Dictionary<string, Func<PersonBaseCreditReportService>>(StringComparer.InvariantCultureIgnoreCase)
            {
                { ProviderNames.BisnodeFi, () => NEnv.IsProduction ? (PersonBaseCreditReportService)new BisnodeService(ProviderNames.BisnodeFi) : new TestCreditReportService(new BisnodeService(ProviderNames.BisnodeFi)) },
                { ProviderNames.UcSe, () => CreateServiceWithTestSupport(() => new UcService(ProviderNames.UcSe), () => NEnv.UcSe) },
                { ProviderNames.CreditSafeSe, () => CreateServiceWithTestSupport(() => new CreditSafeSeService(ProviderNames.CreditSafeSe), () => NEnv.CreditSafeSe )},
                { ProviderNames.TestFi, () =>
                    {
                        if(NEnv.IsProduction)
                            throw new Exception($"Provider {ProviderNames.TestFi} not allowed in production");
                        else
                            return new TestCreditReportService(ProviderNames.TestFi, "FI");
                    }
                },
                { ProviderNames.TestSe, () =>
                    {
                        if(NEnv.IsProduction)
                            throw new Exception($"Provider {ProviderNames.TestSe} not allowed in production");
                        else
                            return new TestCreditReportService(ProviderNames.TestSe, "SE");
                    }
                },
                { ProviderNames.TestOnlyFi, () =>
                    {
                        if(NEnv.IsProduction)
                            throw new Exception($"Provider {ProviderNames.TestOnlyFi} not allowed in production");
                        else
                            return new TestOnlyCreditReportService(ProviderNames.TestOnlyFi, "FI", new DocumentClient());
                    }
                },
                { ProviderNames.TestOnlySe, () =>
                    {
                        if(NEnv.IsProduction)
                            throw new Exception($"Provider {ProviderNames.TestOnlySe} not allowed in production");
                        else
                        {
                            var tp = new TestOnlyCreditReportService(ProviderNames.TestOnlySe, "SE", new DocumentClient());
                            if(NEnv.UcSe == null)
                                return tp;
                            else
                                return new CivicRegNrSwapTestCreditReportService(new UcService(ProviderNames.UcSe), tp);
                        }
                    }
                },
                { ProviderNames.SatFi, () =>
                    {
                        var satAccount = NEnv.SatAccount;
                        if(NEnv.IsProduction)
                            return new SatFiService(true, satAccount);
                        else if(satAccount != null)
                            return new SatFiService(false, satAccount);
                        else if(NEnv.ServiceRegistry.ContainsService("nTest"))
                            return new TestModuleSatFiService();
                        else
                            return new SatFiAllZeroTestService();
                    }
                },
                { ProviderNames.SatFiCreditReport, () =>
                {
                    var satAccount = NEnv.SatFiCreditReportAccount;

                    return new SatFiCreditReportService(NEnv.IsProduction, new DocumentClient(), satAccount);
                }}
            };
        });

        public static bool Exists(string providerName)
        {
            return serviceFactoryByName.Value.ContainsKey(providerName);
        }

        public static PersonBaseCreditReportService Create(string providerName)
        {
            return serviceFactoryByName.Value[providerName]();
        }
    }
}