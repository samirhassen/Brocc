using nCustomer.DbModel;
using Newtonsoft.Json;
using NTech;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System.Linq;

namespace nCustomer.Code.Services.Kyc
{
    public static class KycQuestionsBalanziaSeMigrator
    {
        public static int MigrateCompanyLoanQuestionSets(INTechCurrentUserMetadata user, IClock clock)
        {
            if (NEnv.ClientCfg.ClientName != "balanziaSe")
                return 0;
            /*
             * Fixes a design mistake where kyc question sets where stored on the customer but not in StoredCustomerQuestionSets
             * for company loans
            */

            using (var context = new CustomersContext())
            {
                var answers = context
                    .CustomerProperties
                    .Where(x => x.Name == "latestCustomerQuestionsSetKey" && x.IsCurrentData == true && !context.StoredCustomerQuestionSets.Any(y => y.CustomerId == x.CustomerId))
                    .Select(x => new { x.IsEncrypted, x.CustomerId, x.Value })
                    .ToList();

                var decryptedValues = EncryptionContext.Load(context,
                    answers.Where(x => x.IsEncrypted).Select(x => long.Parse(x.Value)).ToArray(), NEnv.EncryptionKeys.AsDictionary());

                var count = 0;
                foreach (var answer in answers)
                {
                    var answersKey = answer.IsEncrypted ? decryptedValues[long.Parse(answer.Value)] : answer.Value;

                    if (string.IsNullOrWhiteSpace(answersKey))
                        continue;

                    var customerQuestionsSetRaw = context.KeyValueItems.SingleOrDefault(x => x.Key == answersKey && x.KeySpace == "CustomerQuestionsSetV1")?.Value;
                    var customerQuestionsSet = JsonConvert.DeserializeObject<CustomerQuestionsSet>(customerQuestionsSetRaw);
                    if (customerQuestionsSet == null)
                        continue;

                    context.StoredCustomerQuestionSets.Add(new StoredCustomerQuestionSet
                    {
                        AnswerDate = customerQuestionsSet.AnswerDate ?? clock.Today,
                        CustomerId = customerQuestionsSet.CustomerId.Value,
                        ChangedById = user.UserId,
                        InformationMetaData = user.InformationMetadata,
                        ChangedDate = clock.Now,
                        KeyValueStorageKeySpace = CustomerQuestionsSet.KeyValueStoreKeySpaceName,
                        KeyValueStorageKey = answersKey,
                        SourceType = "Migrated",
                        SourceId = "Migrated"
                    });
                    count++;
                }
                context.SaveChanges();
                return count;
            }
        }

    }
}