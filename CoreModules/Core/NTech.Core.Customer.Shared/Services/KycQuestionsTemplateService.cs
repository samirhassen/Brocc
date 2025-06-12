using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using nCustomer.DbModel;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Customer.Shared.Models;
using NTech.Core.Module.Shared.Infrastructure;

namespace NTech.Core.Customer.Shared.Services
{
    public class KycQuestionsTemplateService
    {
        private readonly CustomerContextFactory _contextFactory;
        private readonly ICustomerEnvSettings _envSettings;
        private readonly IClientConfigurationCore _clientConfiguration;

        public KycQuestionsTemplateService(CustomerContextFactory contextFactory, ICustomerEnvSettings envSettings,
            IClientConfigurationCore clientConfiguration)
        {
            _contextFactory = contextFactory;
            _envSettings = envSettings;
            _clientConfiguration = clientConfiguration;
        }

        private HashSet<string> GetActiveRelationTypes()
        {
            var relationTypes = new HashSet<string>();

            if (_clientConfiguration.IsFeatureEnabled("ntech.feature.unsecuredloans"))
            {
                relationTypes.Add("Credit_UnsecuredLoan");
            }

            if (_clientConfiguration.IsFeatureEnabled("ntech.feature.savings"))
            {
                relationTypes.Add("SavingsAccount_StandardAccount");
            }

            if (_clientConfiguration.IsFeatureEnabled("ntech.feature.mortgageloans"))
            {
                relationTypes.Add("Credit_MortgageLoan");
            }

            if (_clientConfiguration.IsFeatureEnabled("ntech.feature.companyloans"))
            {
                relationTypes.Add("Credit_CompanyLoan");
            }

            return relationTypes;
        }

        public KycQuestionsTemplateInitialDataResponse GetInitialData()
        {
            using (var context = _contextFactory.CreateContext())
            {
                var templates = context
                    .KycQuestionTemplatesQueryable
                    .Where(x => x.RemovedByUserId == null)
                    .Select(x => new
                    {
                        x.Id,
                        x.CreatedDate,
                        x.RelationType
                    })
                    .ToList();

                var activeRelationTypes = GetActiveRelationTypes();
                var defaultQuestionSetsByRelationType = _envSettings.DefaultKycQuestionsSets ??
                                                        new Dictionary<string, KycQuestionsTemplate>();
                foreach (var nonActiveStandardRelationType in defaultQuestionSetsByRelationType.Keys
                             .Except(activeRelationTypes).ToList())
                {
                    defaultQuestionSetsByRelationType.Remove(nonActiveStandardRelationType);
                }

                var relationTypes = templates
                    .Select(x => x.RelationType)
                    .Concat(activeRelationTypes)
                    .ToHashSetShared();
                var latestIdByRelationType = templates
                    .GroupBy(x => x.RelationType)
                    .ToDictionary(x => x.Key, x => x.Max(y => y.Id));

                Dictionary<string, string> latestQuestionDataByRelationType;
                if (latestIdByRelationType.Count > 0)
                {
                    latestQuestionDataByRelationType = context
                        .KycQuestionTemplatesQueryable
                        .Where(x => latestIdByRelationType.Values.Contains(x.Id))
                        .ToDictionary(x => x.RelationType, x => x.ModelData);
                }
                else
                {
                    latestQuestionDataByRelationType = new Dictionary<string, string>();
                }

                return new KycQuestionsTemplateInitialDataResponse
                {
                    ActiveProducts = relationTypes.Select(relationType =>
                    {
                        var latestQuestionData = latestQuestionDataByRelationType.Opt(relationType);
                        var currentTemplate = latestQuestionData != null
                            ? KycQuestionsTemplate.Parse(latestQuestionData)
                            : defaultQuestionSetsByRelationType.Opt(relationType);

                        return new KycQuestionsTemplateInitialDataResponse.ActiveProductModel
                        {
                            RelationType = relationType,
                            CurrentQuestionsTemplate = currentTemplate,
                            HistoricalModels = templates.Where(x => x.RelationType == relationType)
                                .OrderByDescending(x => x.Id).Select(x =>
                                    new KycQuestionsTemplateInitialDataResponse.HistoricalModel
                                    {
                                        Id = x.Id,
                                        Date = x.CreatedDate
                                    }).ToList()
                        };
                    }).ToList()
                };
            }
        }

        public SaveQuestionsResponse SaveQuestions(SaveQuestionsRequest request)
        {
            if (!KycQuestionsTemplate.TryParse(request?.ModelData, out var failedMessage, out var questionSet))
            {
                throw new NTechCoreWebserviceException(failedMessage)
                    { IsUserFacing = true, ErrorCode = "invalidQuestionsModel", ErrorHttpStatusCode = 400 };
            }

            questionSet.Version = questionSet.Version ?? Guid.NewGuid().ToString();

            using (var context = _contextFactory.CreateContext())
            {
                var template = new KycQuestionTemplate
                {
                    CreatedByUserId = context.CurrentUser.UserId,
                    CreatedDate = context.CoreClock.Now,
                    ModelData = questionSet.Serialize(),
                    RelationType = request.RelationType
                };
                context.AddKycQuestionTemplates(template);
                context.SaveChanges();

                return new SaveQuestionsResponse
                {
                    Id = template.Id,
                    Version = questionSet.Version
                };
            }
        }

        public GetModelDataResponse GetModelData(GetModelDataRequest request)
        {
            var id = request.Id.Value;
            using (var context = _contextFactory.CreateContext())
            {
                return new GetModelDataResponse
                {
                    ModelData = context.KycQuestionTemplatesQueryable
                        .SingleOrDefault(x => x.RemovedByUserId == null && x.Id == id)?.ModelData
                };
            }
        }

        public ValidateTemplateResponse Validate(ValidateTemplateRequest request)
        {
            var isValid = KycQuestionsTemplate.TryParse(request?.ModelData, out var validationErrorMessage, out var _);
            return new ValidateTemplateResponse
            {
                IsValid = isValid,
                ValidationErrorMessage = validationErrorMessage,
            };
        }
    }

    public class GetModelDataRequest
    {
        [Required] public int? Id { get; set; }
    }

    public class GetModelDataResponse
    {
        public string ModelData { get; set; }
    }

    public class SaveQuestionsRequest
    {
        [Required] public string RelationType { get; set; }
        [Required] public string ModelData { get; set; }
    }

    public class SaveQuestionsResponse
    {
        public int Id { get; set; }
        public string Version { get; set; }
    }

    public class KycQuestionsTemplateInitialDataResponse
    {
        public List<ActiveProductModel> ActiveProducts { get; set; }

        public class ActiveProductModel
        {
            public string RelationType { get; set; }
            public KycQuestionsTemplate CurrentQuestionsTemplate { get; set; }
            public List<HistoricalModel> HistoricalModels { get; set; }
        }

        public class HistoricalModel
        {
            public int Id { get; set; }
            public DateTimeOffset Date { get; set; }
        }
    }

    public class ValidateTemplateRequest
    {
        public string ModelData { get; set; }
    }

    public class ValidateTemplateResponse
    {
        public bool IsValid { get; set; }
        public object ValidationErrorMessage { get; set; }
    }
}