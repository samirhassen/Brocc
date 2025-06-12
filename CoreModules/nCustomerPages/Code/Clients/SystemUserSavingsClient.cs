using System;
using System.Collections.Generic;
using System.Linq;

namespace nCustomerPages.Code
{
    public class SystemUserSavingsClient : AbstractSystemUserServiceClient
    {
        protected override string ServiceName => "nSavings";

        public const string StandardAccountTypeCode = "StandardAccount";

        public string StoreTemporarilyEncryptedData(string plaintextMessage, int? expireAfterHours = null)
        {
            return Begin().PostJson("Api/EncryptedTemporaryStorage/StoreString", new
            {
                plaintextMessage = plaintextMessage,
                expireAfterHours = expireAfterHours
            }).ParseJsonAsAnonymousType(new { compoundKey = (string)null })?.compoundKey;
        }

        public bool TryGetTemporarilyEncryptedData(string compoundKey, out string plaintextMessage)
        {
            var result = Begin().PostJson("Api/EncryptedTemporaryStorage/GetString", new
            {
                compoundKey = compoundKey
            }).ParseJsonAsAnonymousType(new { exists = (bool?)null, plaintextMessage = (string)null });

            if (result?.exists ?? false)
            {
                plaintextMessage = result?.plaintextMessage;
                return true;
            }
            else
            {
                plaintextMessage = null;
                return false;
            }
        }

        public class CreateAccountResult
        {
            public string SavingsAccountNr { get; set; }
            public string Status { get; set; }
        }

        public CreateAccountResult CreateAccount(
            IList<Tuple<string, string>> applicationItems,
            IList<Tuple<string, string>> externalVariables)
        {
            return Begin()
                .PostJson("Api/SavingsAccount/Create", new
                {
                    applicationItems = applicationItems?.Select(x => new { Name = x.Item1, Value = x.Item2 }).ToList(),
                    externalVariables = externalVariables?.Select(x => new { Name = x.Item1, Value = x.Item2 }).ToList(),
                })
                .ParseJsonAs<CreateAccountResult>();
        }

        public string CreateNewSavingsAccountNumber()
        {
            var nr = Begin()
                .PostJson("Api/NewSavingsAccountNumber", new { })
                .ParseJsonAsAnonymousType(new { nr = (string)null })
                ?.nr;
            if (string.IsNullOrWhiteSpace(nr))
                throw new Exception("No nr back");
            return nr;
        }

        public class SavingsAccountStatusItem
        {
            public int MainCustomerId { get; set; }
            public int CreatedByBusinessEventId { get; set; }
            public string SavingsAccountNr { get; set; }
            public string AccountStatus { get; set; }
        }

        public IDictionary<int, IList<SavingsAccountStatusItem>> GetSavingsAccountStatus(ISet<int> customerIds)
        {
            return Begin()
                .PostJson("Api/SavingsAccount/GetStatusByCustomerIds", new { customerIds = customerIds?.ToList() })
                .ParseJsonAs<IDictionary<int, IList<SavingsAccountStatusItem>>>()
                ?? new Dictionary<int, IList<SavingsAccountStatusItem>>();
        }

        public bool HasOrHasEverHadASavingsAccount(int customerId)
        {
            var status = GetSavingsAccountStatus(new HashSet<int> { customerId });
            return status.Count > 0 && status.ContainsKey(customerId) && status[customerId].Count > 0;

        }

        public bool TryGetCurrentInterestRateForStandardAccount(out decimal interestRatePercent)
        {
            var result = Begin()
                .PostJson("Api/InterestRate/FetchCurrentByAccountTypeCode", new { savingsAccountTypeCode = "StandardAccount" })
                .ParseJsonAsAnonymousType(new
                {
                    HasRate = default(bool),
                    Rate = new
                    {
                        InterestRatePercent = default(decimal)
                    }
                });

            if (result == null || !result.HasRate)
            {
                interestRatePercent = 0;
                return false;
            }

            interestRatePercent = result.Rate.InterestRatePercent;

            return true;
        }
    }
}