using NTech.Core.Module.Shared.Clients;

namespace NTech.Core.Host.IntegrationTests.UlLegacy.Utilities
{
    internal static class KycAnswersUlLegacy
    {
        public static List<CustomerQuestionsSetItem> CreateApplicationAnswers(SupportShared support)
        {
            return QuestionsFromDict(new Dictionary<string, string>
            {
                ["loan_purpose"] = "consumption",
                ["loan_whosmoney"] = "own",
                ["loan_paymentfrequency"] = "onschedule",
                ["ispep"] = "false",
                //On ispep = true, pepRoles = governmentofficial
                ["hasOtherCitizenCountry"] = "false",
                ["hasOtherTaxCountry"] = "false"
            });
        }

        private static List<CustomerQuestionsSetItem> QuestionsFromDict(Dictionary<string, string> answers)
        {
            return answers.Keys.Select(questionCode => new CustomerQuestionsSetItem
            {
                AnswerCode = answers[questionCode],
                QuestionCode = questionCode,
                AnswerText = $"T {answers[questionCode]}",
                QuestionText = $"Q {questionCode}"
            }).ToList();
        }
    }
}
