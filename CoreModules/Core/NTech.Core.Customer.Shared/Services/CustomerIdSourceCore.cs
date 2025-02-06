using Dapper;
using NTech.Banking.CivicRegNumbers;
using NTech.Banking.OrganisationNumbers;
using NTech.Core.Module.Shared.Database;
using System;
using System.Linq;

namespace NTech.Core.Customer.Shared.Services
{
    public class CustomerIdSourceCore
    {
        public static int GetCustomerIdByCivicRegNr(ICivicRegNumber civicRegNr, INTechDbContext context)
        {
            if (civicRegNr == null)
                throw new ArgumentNullException("civicRegNr");
            return GetCustomerIdByNr(civicRegNr.NormalizedValue, context: context);
        }

        public static int GetCustomerIdByOrgnr(IOrganisationNumber orgnr, INTechDbContext context)
        {
            if (orgnr == null)
                throw new ArgumentNullException("orgnr");
            //We use a prefix to allow the things like swedish enskild firma to exist as both a private person with civicregnr and a company with the same orgnr but different customer ids
            return GetCustomerIdByNr($"C" + orgnr.NormalizedValue, context: context);
        }

        private static int GetCustomerIdByNr(string nr, INTechDbContext context)
        {
            if (nr == null)
                throw new ArgumentNullException("nr");

            var hash = CustomerServiceBase.ComputeCustomerCivicOrOrgnrToCustomerIdMappingHash(nr);
            return context.GetConnection().Query<int>(CustomerServiceBase.CreateCustomerIdSql, param: new { hash }).Single();
        }
    }
}