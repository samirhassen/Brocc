using nCustomer.DbModel;
using NTech.Banking.CivicRegNumbers;
using NTech.Banking.OrganisationNumbers;
using NTech.Core.Customer.Shared.Services;
using NTech.Core.Module.Shared.Database;
using System;

namespace nCustomer
{
    public class CustomerIdSource
    {
        public static int GetCustomerIdByCivicRegNr(ICivicRegNumber civicRegNr, INTechDbContext context = null) =>
            WithContext(x => CustomerIdSourceCore.GetCustomerIdByCivicRegNr(civicRegNr, x), context);

        public static int GetCustomerIdByOrgnr(IOrganisationNumber orgnr, INTechDbContext context = null) =>
            WithContext(x => CustomerIdSourceCore.GetCustomerIdByOrgnr(orgnr, x), context);

        private static T WithContext<T>(Func<INTechDbContext, T> exec, INTechDbContext context)
        {
            if (context != null)
                return exec(context);
            else
                using (var ctx = new CustomersContext())
                {
                    return exec(ctx);
                }
        }
    }
}