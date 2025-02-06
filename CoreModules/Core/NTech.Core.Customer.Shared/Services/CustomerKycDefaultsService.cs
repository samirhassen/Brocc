using nCustomer;
using nCustomer.Code.Services;
using nCustomer.Code.Services.Kyc;
using Newtonsoft.Json;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Core.Customer.Shared.Services
{
    public class CustomerKycDefaultsService
    {
        private readonly KycManagementService kycManagementService;
        private readonly CustomerPropertyStatusService propertyStatusService;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly CustomerContextFactory contextFactory;
        private readonly Func<ICustomerContextExtended, INTechCurrentUserMetadata, CustomerWriteRepository> createRepository;

        public CustomerKycDefaultsService(KycManagementService kycManagementService, CustomerPropertyStatusService propertyStatusService,
            IClientConfigurationCore clientConfiguration, CustomerContextFactory contextFactory,
            Func<ICustomerContextExtended, INTechCurrentUserMetadata, CustomerWriteRepository> createRepository)
        {
            this.kycManagementService = kycManagementService;
            this.propertyStatusService = propertyStatusService;
            this.clientConfiguration = clientConfiguration;
            this.contextFactory = contextFactory;
            this.createRepository = createRepository;
        }

        public SetupCustomerKycDefaultsResponse SetupCustomerKycDefaults(SetupCustomerKycDefaultsRequest request)
        {
            if (request == null)
                throw new NTechCoreWebserviceException("Missing request") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

            var onboardingStatusByCustomerId = kycManagementService.FetchKycCustomerOnboardingStatuses(request.CustomerIds.ToHashSetShared(),
                (string.IsNullOrWhiteSpace(request.OnlyThisSourceType) && string.IsNullOrWhiteSpace(request.OnlyThisSourceId))
                    ? ((string SourceType, string SourceId)?)null
                    : (SourceType: request.OnlyThisSourceType, SourceId: request.OnlyThisSourceId), true);

            var customerItemsToAdd = new List<CustomerPropertyModel>();

            string GetAnswerCode(string questionCode, CustomerQuestionsSet questionSet) =>
                questionSet?.Items?.FirstOrDefault(x => x.QuestionCode == questionCode)?.AnswerCode;

            if (!onboardingStatusByCustomerId.Values.All(x => x.LatestKycQuestionsAnswerDate.HasValue))
                return new SetupCustomerKycDefaultsResponse { HaveAllCustomersAnsweredQuestions = false };

            foreach (var customerId in request.CustomerIds)
            {
                var latestKycQuestionsSet = onboardingStatusByCustomerId[customerId].LatestKycQuestionsSet;


                var propertyStatus = new Lazy<CustomerPropertyStatusService.CheckPropertyStatusResult>(() =>
                {
                    if (!propertyStatusService.TryCheckPropertyStatus(customerId, new List<string> { "includeInFatcaExport", "taxcountries", "citizencountries" }, out var failedMessage, out var result))
                    {
                        throw new Exception(failedMessage);
                    }
                    return result;
                });

                void SetDefaultIfMissing(int customerIdLocal, string groupName, string propertyName, Func<string> propertyValue)
                {
                    if (propertyStatus.Value.MissingPropertyNames?.Any(x => x == propertyName) == true)
                    {
                        customerItemsToAdd.Add(new CustomerPropertyModel
                        {
                            CustomerId = customerId,
                            Group = groupName,
                            IsSensitive = false,
                            Name = propertyName,
                            Value = propertyValue()
                        });
                    }
                }

                Func<string> defaultTaxCountries = () => JsonConvert.SerializeObject(new[] { new { countryIsoCode = clientConfiguration.Country.BaseCountry } });
                Func<string> defaultCitizenCountries = () => JsonConvert.SerializeObject(new[] { clientConfiguration.Country.BaseCountry });

                var hasOtherTaxOrCitizenCountry = GetAnswerCode("hasOtherTaxOrCitizenCountry", latestKycQuestionsSet);
                var hasOtherTaxCountry = GetAnswerCode("hasOtherTaxCountry", latestKycQuestionsSet);
                var hasOtherCitizenCountry = GetAnswerCode("hasOtherCitizenCountry", latestKycQuestionsSet);
                if (hasOtherTaxOrCitizenCountry == "false" || hasOtherTaxCountry == "false")
                {
                    SetDefaultIfMissing(customerId, "fatca", "includeInFatcaExport", () => "false");
                    SetDefaultIfMissing(customerId, "taxResidency", "taxcountries", defaultTaxCountries);
                }
                if (hasOtherTaxOrCitizenCountry == "false" || hasOtherCitizenCountry == "false")
                {
                    SetDefaultIfMissing(customerId, "fatca", "citizencountries", defaultCitizenCountries);
                }

                if (customerItemsToAdd.Any())
                {
                    using (var db = contextFactory.CreateContext())
                    {
                        db.BeginTransaction();
                        try
                        {
                            var repository = createRepository(db, db.CurrentUser);
                            repository.UpdateProperties(customerItemsToAdd, false);
                            db.SaveChanges();
                            db.CommitTransaction();
                        }
                        catch
                        {
                            db.RollbackTransaction();
                            throw;
                        }
                    }
                }
            }
            return new SetupCustomerKycDefaultsResponse { HaveAllCustomersAnsweredQuestions = true };
        }
    }
}
