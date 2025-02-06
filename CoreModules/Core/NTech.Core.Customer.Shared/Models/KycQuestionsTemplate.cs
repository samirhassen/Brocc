using Newtonsoft.Json;
using NTech.Core.Module.Shared.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Core.Customer.Shared.Models
{
    public class KycQuestionsTemplate
    {
        private const string QuestionTypeYesNo = "yesNo";
        private const string QuestionTypeDropdown = "dropdown";
        private const string QuestionTypeYesNoWithOptions = "yesNoWithOptions";
        private const string QuestionTypeYesNoWithCountryOptions = "yesNoWithCountryOptions";
        private static HashSet<string> AllQuestionTypes = new HashSet<string>
        {
            QuestionTypeYesNo,
            QuestionTypeDropdown,
            QuestionTypeYesNoWithOptions,
            QuestionTypeYesNoWithCountryOptions,
        };

        public string Version { get; set; }
        public List<KycUiQuestion> Questions { get; set; }

        public class KycUiQuestion
        {
            public string Type { get; set; }
            public string Key { get; set; }
            public Dictionary<string, string> HeaderTranslations { get; set; }
            public string OptionsKey { get; set; }
            public Dictionary<string, string> OptionsHeaderTranslations { get; set; }
            public List<Option> Options { get; set; }
            public class Option
            {
                public string Value { get; set; }
                public Dictionary<string, string> Translations { get; set; }
            }
        }

        public string Serialize()
        {
            var s = new JsonSerializerSettings();
            s.NullValueHandling = NullValueHandling.Ignore;
            return JsonConvert.SerializeObject(this, s);
        }

        public static KycQuestionsTemplate Parse(string modelData)
        {
            if (!TryParse(modelData, out var errorMessage, out var result))
            {
                throw new NTechCoreWebserviceException(errorMessage) { IsUserFacing = true, ErrorHttpStatusCode = 400 };
            }
            return result;
        }

        public static Dictionary<string, KycQuestionsTemplate> ParseDefaultSetting(string fileContent)
        {
            var result = JsonConvert.DeserializeObject<Dictionary<string, KycQuestionsTemplate>>(fileContent);

            if (result == null)
            {
                throw new NTechCoreWebserviceException("Default KYC questions set is empty");
            }

            foreach (var relationType in result.Keys)
            {
                var validationErrorMessage = Validate(result[relationType]);
                if (validationErrorMessage != null)
                    throw new NTechCoreWebserviceException($"Invalid default kyc questions for '{relationType}': {validationErrorMessage}");
            }

            return result;
        }

        public static string Validate(KycQuestionsTemplate questionsSet)
        {
            bool IsValidTranslationDictionary(Dictionary<string, string> translations)
            {
                return translations != null
                    && translations.Count > 0
                    && translations.All(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Value));
            }

            if (questionsSet?.Questions == null || questionsSet.Questions.Count == 0)
            {
                return "Questions model does not contain any questions";
            }

            var allKeys = questionsSet.Questions.Select(x => x.Key).ToList();
            if (allKeys.Any(string.IsNullOrWhiteSpace))
            {
                return "Missing question keys";
            }

            var distinctKeys = allKeys.ToHashSetShared();
            if (allKeys.Count != distinctKeys.Count)
            {
                return "Duplicate question keys";
            }

            var optionKeys = questionsSet.Questions.Where(x => x.Type.IsOneOf(QuestionTypeYesNoWithOptions, QuestionTypeYesNoWithCountryOptions)).Select(x => x.OptionsKey).ToList();
            if (optionKeys.Any(string.IsNullOrWhiteSpace))
            {
                return "Missing option question keys";
            }
            var distinctOptionKeys = optionKeys.ToHashSetShared();
            if (optionKeys.Count != distinctOptionKeys.Count || distinctKeys.Intersect(distinctOptionKeys).Any())
            {
                return "Duplicate option question keys";
            }

            foreach (var question in questionsSet.Questions)
            {
                if (!IsValidTranslationDictionary(question.HeaderTranslations))
                {
                    return "Missing question header translations";
                }

                if (!question.Type.IsOneOf(QuestionTypeDropdown, QuestionTypeYesNoWithOptions, QuestionTypeYesNo, QuestionTypeYesNoWithCountryOptions))
                {
                    return $"Invalid question type. Must be one of: {string.Join("|", AllQuestionTypes)}";
                }

                if (question.Type.IsOneOf(QuestionTypeDropdown, QuestionTypeYesNoWithOptions))
                {
                    if (question.Options == null || question.Options.Count == 0)
                    {
                        return "Missing question options";
                    }
                    foreach (var option in question.Options)
                    {
                        if (string.IsNullOrWhiteSpace(option?.Value))
                        {
                            return "Missing value for option";
                        }
                        if (!IsValidTranslationDictionary(option.Translations))
                        {
                            return "Missing question options translations";
                        }
                    }
                }

                if (question.Type.IsOneOf(QuestionTypeYesNoWithOptions, QuestionTypeYesNoWithCountryOptions))
                {
                    if (!IsValidTranslationDictionary(question.OptionsHeaderTranslations))
                    {
                        return "Missing question options header translations";
                    }
                }
            }

            return null;
        }

        public static bool TryParse(string modelData, out string validationErrorMessage, out KycQuestionsTemplate questionsSet)
        {
            validationErrorMessage = null;
            questionsSet = null;

            try
            {
                questionsSet = JsonConvert.DeserializeObject<KycQuestionsTemplate>(modelData);

                validationErrorMessage = Validate(questionsSet);

                return validationErrorMessage == null;
            }
            catch
            {
                validationErrorMessage = "Questions model is invalid";
                return false;
            }
        }
    }
}
