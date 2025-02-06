using Dapper;
using Newtonsoft.Json;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Core.Customer.Shared.Services.Aml.Cm1
{
    public class Cm1CustomerKycQuestionsRepository
    {
        private readonly CustomerContextFactory contextFactory;
        private readonly ILoggingService loggingService;

        public Cm1CustomerKycQuestionsRepository(CustomerContextFactory contextFactory, ILoggingService loggingService)
        {
            this.contextFactory = contextFactory;
            this.loggingService = loggingService;
        }

        public Dictionary<int, List<Cm1KycAnswerCustomerProperty>> GetLatestAnswersCustomerPropertiesPerCustomer(HashSet<int> customerIds)
        {
            var result = new Dictionary<int, List<Cm1KycAnswerCustomerProperty>>(customerIds.Count);
            using (var context = contextFactory.CreateContext())
            {
                var connection = context.GetConnection();
                foreach (var customerIdGroup in customerIds.ToArray().SplitIntoGroupsOfN(25))
                {
                    var groupCustomerIds = customerIdGroup.ToList();
                    var latestAnswersForGroup = connection.Query<Cm1QueryData>(@"with LatestByCustomerId
as
(
	select	
			t.*,
			rank() over (PARTITION BY t.CustomerId order by t.Id desc) as CustomerRank
	from	StoredCustomerQuestionSet t
)
select	t.CustomerId, k.[Value] as LatestQuestionsJson
from	LatestByCustomerId t
join	KeyValueItem k on k.KeySpace = t.KeyValueStorageKeySpace and k.[Key] = t.KeyValueStorageKey
where	t.CustomerRank = 1
and     t.CustomerId in @customerIds", param: new { customerIds = groupCustomerIds }).ToList();

                    foreach(var answer in latestAnswersForGroup)
                    {
                        try
                        {
                            var latestAnswers = JsonConvert.DeserializeObject<CustomerQuestionsSet>(answer.LatestQuestionsJson);
                            result[answer.CustomerId] = latestAnswers.Items.Select(x => new Cm1KycAnswerCustomerProperty
                            {
                                QuestionPropertyName = x.QuestionCode,
                                QuestionPropertyValue = x.AnswerCode
                            }).ToList();
                        }
                        catch(Exception ex)
                        {
                            var text = $"Failed to parse latest kyc answer for customer {answer.CustomerId}";
                            if (DisableErrorSupression)
                            {
                                throw new Exception(text, ex);
                            }
                            else
                            {
                                loggingService.Warning(ex, text);
                            }                            
                        }
                    }
                }
                return result;
            }
        }

        public class Cm1KycAnswerCustomerProperty
        {
            public string QuestionPropertyName { get; set; }
            public string QuestionPropertyValue { get; set; }
        }

        private class Cm1QueryData
        {
            public int CustomerId { get; set; }
            public string LatestQuestionsJson { get; set; }
        }

        //Used to make testing easiser
        public static bool DisableErrorSupression { get; set; }
    }
}
