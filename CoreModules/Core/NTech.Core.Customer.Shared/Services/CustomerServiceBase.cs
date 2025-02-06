using nCustomer;
using nCustomer.DbModel;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace NTech.Core.Customer.Shared.Services
{
    public abstract class CustomerServiceBase
    {
        protected Exception CreateWebserviceException(string errorMessage) => new NTechCoreWebserviceException(errorMessage);

        protected void CheckForBannedProperties(IDictionary<string, string> properties)
        {
            var bp = properties.Where(x => bannedProperties.Contains(x.Key)).ToList();
            if (bp.Any())
                throw CreateWebserviceException($"Properties not allowed: {string.Join(", ", bp)}");
        }

        private static HashSet<string> bannedProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            CustomerProperty.Codes.civicRegNr.ToString(),
            CustomerProperty.Codes.civicregnr_country.ToString(),
            CustomerProperty.Codes.orgnr.ToString(),
            CustomerProperty.Codes.orgnr_country.ToString(),
            CustomerProperty.Codes.isCompany.ToString()
        };

        /// <summary>
        /// This hash is used to make sure that the same orgnr/civicnr is always mapped to the same customerId
        /// while not allowing getting orgnr/civicnr directly from customerId.
        /// 
        /// It's very important that this hash does not change over time so be really careful if changing this.
        /// </summary>
        public static string ComputeCustomerCivicOrOrgnrToCustomerIdMappingHash(string nr)
        {
            using (var h = SecurityDriven.Inferno.SuiteB.HashFactory())
            {
                return Convert.ToBase64String(h.ComputeHash(SecurityDriven.Inferno.Utils.SafeUTF8.GetBytes(nr)));
            }
        }

        public static string CreateCustomerIdSql = @"DECLARE @Id [int];
                MERGE CustomerIdSequence WITH (HOLDLOCK) as t
                USING (VALUES (@Hash)) AS Foo (CivicRegNrHash) 
                ON t.CivicRegNrHash = Foo.CivicRegNrHash
                WHEN MATCHED THEN UPDATE SET @Id = t.CustomerId
                WHEN NOT MATCHED THEN INSERT (CivicRegNrHash) VALUES (foo.CivicRegNrHash);
                IF @Id IS NULL
                BEGIN
                    SELECT @Id = CAST(SCOPE_IDENTITY() as [int]);
                END;
                select @Id;";

        public static string ComputeAddressHash(IList<CustomerPropertyModel> items)
        {
            Func<string[], string> getHash = input =>
            {
                var s = string.Join("", input.Select(x => x?.Trim()?.ToLowerInvariant()));
                MD5 md5Hash = MD5.Create();
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(s));
                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }
                return sBuilder.ToString();
            };

            string addressStreet = (items
               .Where(x => x.Name == "addressStreet")
               .SingleOrDefault() ?? new CustomerPropertyModel()).Value;
            string addressZipcode = (items
               .Where(x => x.Name == "addressZipcode")
               .SingleOrDefault() ?? new CustomerPropertyModel()).Value;
            string addressCity = (items
               .Where(x => x.Name == "addressCity")
               .SingleOrDefault() ?? new CustomerPropertyModel()).Value;
            string addressCountry = (items
               .Where(x => x.Name == "addressCountry")
               .SingleOrDefault() ?? new CustomerPropertyModel()).Value;

            return getHash(new[] { addressStreet, addressZipcode, addressCity, addressCountry });
        }
    }
}
