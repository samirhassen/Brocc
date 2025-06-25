using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using NTech.Banking.CivicRegNumbers;
using NTech.Banking.Shared.BankAccounts.Fi;

namespace NTech.Core.Savings.Shared.Services.FinnishCustomsAccounts
{
    public class FinnishCustomsFileFormat
    {
        private readonly KeyValueStoreService keyValueStoreService;

        public FinnishCustomsFileFormat(KeyValueStoreService keyValueStoreService)
        {
            this.keyValueStoreService = keyValueStoreService;
        }

        /// <summary>
        /// This will setup the data according to the specification for endpoint /v2/report-update/cat-1/. 
        /// See more here: https://github.com/FinnishCustoms-SuomenTulli/account-register-information-update/blob/master/index_en.md
        /// Json-schema here: https://github.com/FinnishCustoms-SuomenTulli/account-register-information-update/blob/master/schemas/information_update-v2-credit_institution.json
        /// </summary>
        /// <param name="model">Model with data from the database. </param>
        /// <returns>JObject with data according to Tulli specification. </returns>
        /// <exception cref="Exception"></exception>
        public JObject CreateUpdateFileRaw(UpdateModel model)
        {
            var m = new JObject();

            m.Add("creationDateTime", model.CreationDate.Value.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture));
            m.Add("senderBusinessId", model.SenderBusinessId);

            var uuidsByCustomerId = CreateCustomerIdToTulliUuidMapping(model.Customers.Select(x => x.CustomerId.Value).ToHashSetShared());
            if (model.Customers.Any())
            {
                var legalPersonsNode = new JObject();
                var customersNode = new JObject();

                foreach (var c in model.Customers)
                {
                    var customerUuid = uuidsByCustomerId[c.CustomerId.Value];
                    legalPersonsNode.Add(customerUuid, JToken.FromObject(new
                    {
                        privatePerson = new
                        {
                            fullName = $"{c.LastName}{(c.LastName != null ? ", " : "")}{c.FirstName}".Trim(),
                            hetu = c.CivicRegNr.NormalizedValue,
                            birthDate = c.BirthDate.Value.ToString("yyyy-MM-dd")
                        }
                    }));

                    var customerAccounts = model
                        .SavingsAccounts
                        .Where(x => x.OwnerCustomerId.Value == c.CustomerId.Value)
                        .ToList();

                    if (customerAccounts.Any())
                    {
                        //So if a customer is active 2011 -> 2012 and then reactivtes in say 2015 its a bit unclear how to report this customer
                        //We are going with preserving the start date here so that will be stable but the enddate will go from 2012 to null in 2015 when the new account is created and then back to say 2016 or so when they close that account
                        var minStartDate = model
                            .SavingsAccounts
                            .Where(x => x.OwnerCustomerId.Value == c.CustomerId.Value)
                            .Min(x => x.StartDate);

                        DateTime? endDate = null;
                        if (customerAccounts.All(x => x.EndDate.HasValue))
                            endDate = customerAccounts.Max(x => x.EndDate);
                    }
                }

                m.Add("legalPersons", legalPersonsNode);
            }
            if (model.SavingsAccounts.Any())
            {
                var uuidsByAccountNr = CreateSavingsAccountNrToTulliUuidMapping(model.SavingsAccounts.Select(x => x.AccountNr).ToHashSetShared());
                var accountsNode = new JObject();
                foreach (var a in model.SavingsAccounts)
                {
                    var accountNode = new JObject();
                    if (a.UseWithdrawalIbanAsId)
                    {
                        accountNode.Add("id", JToken.FromObject(new
                        {
                            iban = a.WithdrawalIban.NormalizedValue
                        }));
                    }
                    else
                    {
                        accountNode.Add("id", JToken.FromObject(new
                        {
                            other = new
                            {
                                id = a.AccountNr,
                                description = "internal account number"
                            }
                        }));
                    }
                    accountNode.Add("openingDate", a.StartDate.Value.ToString("yyyy-MM-dd"));
                    if (a.EndDate.HasValue)
                        accountNode.Add("closingDate", a.EndDate.Value.ToString("yyyy-MM-dd"));

                    var ownerRoleNode = new JObject();
                    ownerRoleNode.Add("legalPersonReference", uuidsByCustomerId[a.OwnerCustomerId.Value]);
                    ownerRoleNode.Add("startDate", a.StartDate.Value.ToString("yyyy-MM-dd"));
                    if (a.EndDate.HasValue)
                        ownerRoleNode.Add("endDate", a.EndDate.Value.ToString("yyyy-MM-dd"));
                    ownerRoleNode.Add("type", "owner");

                    accountNode.Add("roles", new JArray(ownerRoleNode));

                    accountsNode.Add(uuidsByAccountNr[a.AccountNr], accountNode);
                }
                m.Add("accounts", accountsNode);
            }

            return m;
        }

        private Dictionary<string, string> CreateSavingsAccountNrToTulliUuidMapping(ISet<string> savingsAccountNrs)
        {
            return CreateMappingShared("FinnishCustomsAccountsAccountIds1", x => x, x => x, savingsAccountNrs);
        }

        private Dictionary<int, string> CreateCustomerIdToTulliUuidMapping(ISet<int> customerIds)
        {
            return CreateMappingShared("FinnishCustomsAccountsCustomerIds1", x => x.ToString(), int.Parse, customerIds);
        }

        private Dictionary<T, string> CreateMappingShared<T>(string keySpace, Func<T, string> keyToString, Func<string, T> keyFromString, ISet<T> keys)
        {
            // Ensure that we send in all customers here, not only delta, since we should not write over existing customers. 
            var keyToUidMapping = keys.ToDictionary(x => keyToString(x), x => Guid.NewGuid().ToString());
            var v = keyValueStoreService.SetConcurrent(keySpace, keySpace, () => keyToUidMapping, x =>
            {
                foreach (var existingValue in x)
                {
                    //For existing values preserve the current id rather than generating a new one
                    keyToUidMapping[existingValue.Key] = existingValue.Value;
                }
                return keyToUidMapping;
            });

            return v.ToDictionary(x => keyFromString(x.Key), x => x.Value);
        }

        public class UpdateModel
        {
            [Required]
            public DateTime? CreationDate { get; set; }

            [Required]
            public string SenderBusinessId { get; set; }

            [Required]
            public string SystemClientName { get; set; }

            [Required]
            public List<Customer> Customers { get; set; }

            [Required]
            public List<Account> SavingsAccounts { get; set; }
        }

        public class Customer
        {
            [Required]
            public int? CustomerId { get; set; }

            [Required]
            public string FirstName { get; set; }

            [Required]
            public string LastName { get; set; }

            [Required]
            public ICivicRegNumber CivicRegNr { get; set; }

            [Required]
            public DateTime? BirthDate { get; set; }
        }

        public class Account
        {
            [Required]
            public int? OwnerCustomerId { get; set; }

            [Required]
            public string AccountNr { get; set; }

            [Required]
            public DateTime? StartDate { get; set; }

            public DateTime? EndDate { get; set; }

            [Required]
            public IBANFi WithdrawalIban { get; set; }

            public bool UseWithdrawalIbanAsId { get; set; }
        }

        #region "Example file"

        /*
         Example file:

{
"creationDateTime": "2020-03-06T12:50:12.374",
"senderBusinessId": "8428746-6",
"legalPersons": {
"CUSTOMER_1": {
  "privatePerson": {
    "fullName": "Fredlund, Eeva-Sofia",
    "hetu": "010659-1031",
    "birthDate": "1959-06-01"
  }
}
},
"customers": {
"CUSTOMER_1": {
  "startDate": "2000-12-31"
}
},
"accounts": {
"ACCOUNT_1": {
  "id": {
    "iban": "FI8371356610003253"
  },
  "openingDate": "2016-11-30",
  "roles": [{
    "legalPersonReference": "CUSTOMER_1",
    "startDate": "2016-11-30",
    "type": "owner"
  }]
},
"ACCOUNT_2": {
  "id": {
    "other": {
      "id": "HR8320134556",
      "description": "other account id"
    }
  },
  "openingDate": "2010-10-30",
  "closingDate": "2019-01-21",
  "roles": [{
      "legalPersonReference": "CUSTOMER_1",
      "startDate": "2010-11-12",
      "endDate": "2019-01-21",
      "type": "owner"
    }
  ]
}
}
}

         */

        #endregion "Example file"
    }
}