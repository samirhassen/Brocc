using nPreCredit.Code.Services;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code
{
    public class HandlerLimitEngine
    {
        private IList<decimal> limits = new List<decimal>();

        public HandlerLimitEngine(IPreCreditContextFactoryService preCreditContextFactory, IClientConfigurationCore clientConfiguration)
        {
            this.preCreditContextFactory = preCreditContextFactory;
            this.clientConfiguration = clientConfiguration;
        }

        public bool IsAllowed(int userLevel, decimal newLoanAmount, decimal customerCurrentLoanAmount)
        {
            if (userLevel == 0)
                return false; //Everything is always below limit

            var i = userLevel - 1;
            if (i < 0 || i > limits.Count)
                throw new Exception($"No such userLevel {userLevel}");

            if (i == limits.Count)
                return true; //Meaning unlimited
            else
                return (newLoanAmount + customerCurrentLoanAmount) <= limits[i];
        }

        /// <summary>
        /// For example AddHandlerLimits(1000, 2000, 3000) means that level 1 is limited to 1000, level 2 to 2000 and level 3 to 3000 while level 4 and above are unlimited
        /// </summary>
        /// <param name="levelLimits"></param>
        public void AddHandlerLimits(params decimal[] levelLimits)
        {
            var m = 0m;
            foreach (var limit in levelLimits)
            {
                if (!(limit > m))
                    throw new Exception("Limits need to be strictly increasing per level");
                m = limit;
            }
            limits = levelLimits.ToList();
        }

        public static IList<decimal> ParseLimitLevelsFromClientConfig(IClientConfigurationCore clientConfiguration)
        {
            var value = clientConfiguration.OptionalSetting("ntech.handlerlimits.levelamounts");
            if (value == null)
                throw new Exception("Missing client setting ntech.handlerlimits.levelamounts");
            return value
                    .Split(',')
                    .Select(int.Parse)
                    .Select(x => (decimal)x)
                    .ToArray();
        }

        private void GetUserHandlerLimitSettings(int userId, out int level, out bool isAllowedToOverride)
        {
            if (LevelAndAllowedToOverrideByUserId == null)
            {
                using (var context = preCreditContextFactory.CreateExtended())
                {
                    LevelAndAllowedToOverrideByUserId = context.HandlerLimitLevelsQueryable.ToDictionary(x => x.HandlerUserId, x => Tuple.Create(x.LimitLevel, x.IsOverrideAllowed));
                }
            }
            if (LevelAndAllowedToOverrideByUserId.ContainsKey(userId))
            {
                var u = LevelAndAllowedToOverrideByUserId[userId];
                level = u.Item1;
                isAllowedToOverride = u.Item2;
            }
            else
            {
                level = 0;
                isAllowedToOverride = false;
            }
        }

        private Dictionary<int, Tuple<int, bool>> LevelAndAllowedToOverrideByUserId = null;
        private readonly IPreCreditContextFactoryService preCreditContextFactory;
        private readonly IClientConfigurationCore clientConfiguration;

        public void CheckHandlerLimits(decimal additionalLoanAmount, decimal capitalBalance, int handlerUserId, out bool isOverHandlerLimit, out bool? isAllowedToOverrideHandlerLimit)
        {
            isOverHandlerLimit = false;
            isAllowedToOverrideHandlerLimit = null;

            if (clientConfiguration.IsFeatureEnabled("ntech.feature.handlerlimits.v1"))
            {
                GetUserHandlerLimitSettings(handlerUserId, out var userLevel, out var isUserAllowedToOverride);

                AddHandlerLimits(ParseLimitLevelsFromClientConfig(clientConfiguration).ToArray());

                if (!IsAllowed(userLevel, additionalLoanAmount, capitalBalance))
                {
                    isAllowedToOverrideHandlerLimit = isUserAllowedToOverride;
                    isOverHandlerLimit = true;
                }
            }
        }

        public static void CheckIfOverHandlerLimitShared(string applicationNr, decimal newLoanAmount, int currentUserId, out bool isOverHandlerLimit, out bool? isAllowedToOverrideHandlerLimit, IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository, 
            ICreditClient creditClient, IPreCreditContextFactoryService contextFactoryService, IClientConfigurationCore clientConfiguration, ISet<int> customerIdsOverride)
        {
            List<int> customerIds = new List<int>();
            if (customerIdsOverride != null)
            {
                customerIds = customerIdsOverride.ToList();
            }
            else
            {
                customerIds = new List<int>();
                var a = partialCreditApplicationModelRepository.Get(applicationNr, applicantFields: new List<string> { "customerId" });
                a.DoForEachApplicant(applicantNr => customerIds.Add(a.Applicant(applicantNr).Get("customerId").IntValue.Required));
            }

            var capitalBalance = creditClient.GetCustomerCreditHistory(customerIds).Aggregate(0m, (x, y) => x + y.CapitalBalance);

            bool isOverHandlerLimitLocal;
            bool? isAllowedToOverrideHandlerLimitLocal;
            var e = new HandlerLimitEngine(contextFactoryService, clientConfiguration);
            e.CheckHandlerLimits(newLoanAmount, capitalBalance, currentUserId, out isOverHandlerLimitLocal, out isAllowedToOverrideHandlerLimitLocal);

            isOverHandlerLimit = isOverHandlerLimitLocal;
            isAllowedToOverrideHandlerLimit = isAllowedToOverrideHandlerLimitLocal;
        }
    }
}