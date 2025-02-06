using nCustomer.DbModel;
using Newtonsoft.Json;
using NTech.Banking.CivicRegNumbers;
using NTech.Core;
using NTech.Core.Customer.Shared;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace nCustomer.Code.Services.Kyc
{
    public class KycScreeningService : IKycScreeningService
    {
        private readonly Func<ICustomerContext, INTechCurrentUserMetadata, CustomerWriteRepository> createCustomerRepository;
        private readonly ICoreClock clock;
        private readonly Lazy<IKycScreeningProviderServiceFactory> kycServiceFactory;
        private readonly IKycManagementService kycManagementService;
        private readonly INTechCurrentUserMetadata currentUser;
        private readonly CustomerContextFactory customerContextFactory;
        private readonly EncryptionService encryptionService;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly ICustomerEnvSettings envSettings;

        public KycScreeningService(
            Func<ICustomerContext, INTechCurrentUserMetadata, CustomerWriteRepository> createCustomerRepository,
            ICoreClock clock, Lazy<IKycScreeningProviderServiceFactory> kycServiceFactory,
            IKycManagementService kycManagementService, INTechCurrentUserMetadata currentUser,
            CustomerContextFactory customerContextFactory, EncryptionService encryptionService,
            IClientConfigurationCore clientConfiguration, ICustomerEnvSettings envSettings)
        {
            this.createCustomerRepository = createCustomerRepository;
            this.clock = clock;
            this.kycServiceFactory = kycServiceFactory;
            this.kycManagementService = kycManagementService;
            this.currentUser = currentUser;
            this.customerContextFactory = customerContextFactory;
            this.encryptionService = encryptionService;
            this.clientConfiguration = clientConfiguration;
            this.envSettings = envSettings;
        }

        public Tuple<bool, DateTime?> IsCustomerScreened(int customerId, DateTime? screenDate)
        {
            using (var context = customerContextFactory.CreateContext())
            {
                var results = context.TrapetsQueryResultsQueryable.Where(x => x.CustomerId == customerId);
                if (screenDate.HasValue)
                {
                    var d = screenDate.Value.Date;
                    results = results.Where(x => x.QueryDate == d);
                }
                var latestQueryDate = results.OrderByDescending(x => x.QueryDate).Select(x => (DateTime?)x.QueryDate).FirstOrDefault();

                if (!latestQueryDate.HasValue)
                    return Tuple.Create(false, (DateTime?)null);
                else
                    return Tuple.Create(true, latestQueryDate);
            }
        }

        private static IEnumerable<CustomerPropertyModel> GetLocalPepAndSanctionStatusUpdateCustomerItems(
            QueryItemModel queryItem,
            TrapetsQueryResult queryResult,
            Func<int, bool?> getLatestIsPepQuestionAnswer)
        {
            var localIsPepUnknown = !(queryItem.CurrentLocalIsPep ?? "").IsOneOfIgnoreCase("true", "false");

            if (!queryResult.IsPepHit && localIsPepUnknown)
            {
                var shouldUpdate = true;
                var latestPepAnswer = getLatestIsPepQuestionAnswer(queryItem.CustomerId);
                if (latestPepAnswer == true)
                {
                    //The customers answer differs from the api result so leave the value unknown for manual attention
                    shouldUpdate = false;
                }

                if (shouldUpdate)
                {
                    yield return new CustomerPropertyModel
                    {
                        CustomerId = queryItem.CustomerId,
                        Group = CustomerProperty.Groups.pepKyc.ToString(),
                        Name = CustomerProperty.Codes.localIsPep.ToString(),
                        Value = "false",
                        IsSensitive = false,
                        ForceUpdate = true
                    };
                }
            }

            var localIsSanctionUnknown = !(queryItem.CurrentLocalIsSanction ?? "").IsOneOfIgnoreCase("true", "false");

            if (!queryResult.IsSanctionHit && localIsSanctionUnknown)
            {
                //If the current status is unknown and the screening says false we accept that by default
                yield return new CustomerPropertyModel
                {
                    CustomerId = queryItem.CustomerId,
                    Group = CustomerProperty.Groups.sanction.ToString(),
                    Name = CustomerProperty.Codes.localIsSanction.ToString(),
                    Value = "false",
                    IsSensitive = false,
                    ForceUpdate = true
                };
            }
        }

        public ListScreenBatchNewResult ListScreenBatchNew(List<int> customerIds, DateTime screenDate, Func<int, bool?> getLatestIsPepQuestionAnswer = null, bool isNonBatchScreen = false)
        {
            if (customerIds == null || customerIds.Count == 0)
                return new ListScreenBatchNewResult
                {
                    Success = true
                };

            var needsKycScreeningData = new List<int>();
            foreach (var customerIdGroup in customerIds.ToArray().SplitIntoGroupsOfN(500))
            {
                using (var context = customerContextFactory.CreateContext())
                {
                    var hasKycScreeningDataIds = context
                        .TrapetsQueryResultItemsQueryable
                        .Where(x => customerIdGroup.Contains(x.QueryResult.CustomerId) && x.QueryResult.QueryDate == screenDate)
                        .Select(x => x.QueryResult.CustomerId)
                        .ToList();
                    needsKycScreeningData.AddRange(customerIdGroup.Except(hasKycScreeningDataIds));
                }
            }

            var failedToGetKycScreeningData = new List<Tuple<int, string>>();
            DoKycScreenWhenNeeded(needsKycScreeningData, screenDate, (customerId, msg) => failedToGetKycScreeningData.Add(Tuple.Create(customerId, msg)), getLatestIsPepQuestionAnswer: getLatestIsPepQuestionAnswer, isNonBatchScreen: isNonBatchScreen);

            return new ListScreenBatchNewResult
            {
                Success = true,
                FailedToGetTrapetsDataItems = failedToGetKycScreeningData.Select(x => new ListScreenBatchNewResult.FailedItemModel
                {
                    CustomerId = x.Item1,
                    Reason = x.Item2
                }).ToList()
            };
        }

        public enum ListScreenFailedCode
        {
            MissingName,
            InvalidBirthDate,
            ProviderDown,
            InvalidCivicRegNr
        }

        private void DoKycScreenWhenNeeded(List<int> customerIds, DateTime screenDate, Action<int, string> screeningSkippedReason, Func<int, bool?> getLatestIsPepQuestionAnswer = null, bool isNonBatchScreen = false)
        {
            getLatestIsPepQuestionAnswer = getLatestIsPepQuestionAnswer ?? (x => GetLatestIsPepQuestionAnswerDefault(x));

            foreach (var customerIdGroup in customerIds.ToArray().SplitIntoGroupsOfN(100))
            {
                using (var context = customerContextFactory.CreateContext())
                {
                    context.BeginTransaction();
                    try
                    {
                        context.IsChangeTrackingEnabled = false;


                        var customerRepo = createCustomerRepository(context, currentUser);

                        var includeContactInfo = isNonBatchScreen && kycServiceFactory.Value.DoesCurrentProviderSupportContactInfo();
                        var customersResult = GetCustomerDataForCustomerToScreen(customerIdGroup, customerRepo, includeContactInfo);

                        var queryItems = CreateQueryItems(customersResult, screeningSkippedReason, includeContactInfo);

                        if (queryItems.Any())
                        {
                            ScreenQueryItems(queryItems, context, screenDate, customerRepo, getLatestIsPepQuestionAnswer);
                        }

                        context.DetectChanges();
                        context.SaveChanges();
                        context.CommitTransaction();
                    }
                    catch
                    {
                        context.RollbackTransaction();
                        throw;
                    }
                }
            }
        }

        private List<QueryItemModel> CreateQueryItems(IDictionary<int, IList<CustomerPropertyModel>> customersResult, Action<int, string> screeningSkippedReason, bool includeContactInfo)
        {
            var queryItems = new List<QueryItemModel>();
            var parser = new CivicRegNumberParser(clientConfiguration.Country.BaseCountry);

            string GetProp(CustomerProperty.Codes code, KeyValuePair<int, IList<CustomerPropertyModel>> d) =>
                d.Value.FirstOrDefault(x => x.Name == code.ToString())?.Value;

            foreach (var customerDict in customersResult)
            {
                var customerModel = new
                {
                    CustomerId = customerDict.Key,
                    FirstName = GetProp(CustomerProperty.Codes.firstName, customerDict),
                    LastName = GetProp(CustomerProperty.Codes.lastName, customerDict),
                    BirthDate = GetProp(CustomerProperty.Codes.birthDate, customerDict),
                    Email = GetProp(CustomerProperty.Codes.email, customerDict),
                    CivicRegNr = GetProp(CustomerProperty.Codes.civicRegNr, customerDict)
                };

                if (!parser.IsValid(customerModel.CivicRegNr))
                {
                    //Preserving this check from old code but it's hard to see how this could ever happen
                    screeningSkippedReason(customerDict.Key, ListScreenFailedCode.InvalidCivicRegNr.ToString());
                }
                else if (string.IsNullOrWhiteSpace(customerModel.FirstName) && string.IsNullOrWhiteSpace(customerModel.LastName))
                {
                    screeningSkippedReason(customerDict.Key, ListScreenFailedCode.MissingName.ToString());
                }
                else if (!DateTime.TryParseExact(customerModel.BirthDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var birthDate))
                {
                    screeningSkippedReason(customerDict.Key, ListScreenFailedCode.InvalidBirthDate.ToString());
                }
                else
                {
                    queryItems.Add(new QueryItemModel
                    {
                        CustomerId = customerModel.CustomerId,
                        Item = new KycScreeningQueryItem
                        {
                            BirthDate = birthDate,
                            FullName = (customerModel.FirstName + " " + customerModel.LastName).Trim(),
                            ItemId = customerModel.CustomerId.ToString(),
                            TwoLetterIsoCountryCodes = null,
                            CivicRegNr = customerModel.CivicRegNr,
                            FirstName = customerModel.FirstName,
                            LastName = customerModel.LastName,
                            Email = customerModel.Email,
                            ContactInfo = (includeContactInfo && !string.IsNullOrWhiteSpace(GetProp(CustomerProperty.Codes.addressZipcode, customerDict)))
                            ? new KycScreeningQueryItem.ContactInfoModel
                            {
                                StreetAddress = GetProp(CustomerProperty.Codes.addressStreet, customerDict),
                                ZipCode = GetProp(CustomerProperty.Codes.addressZipcode, customerDict),
                                City = GetProp(CustomerProperty.Codes.addressCity, customerDict),
                                Country = GetProp(CustomerProperty.Codes.addressCountry, customerDict),
                                CareOfAddress = null //We dont store it
                            }
                            : null
                        },
                        CurrentLocalIsPep = customersResult[customerModel.CustomerId].FirstOrDefault(x => x.Name == CustomerProperty.Codes.localIsPep.ToString())?.Value,
                        CurrentLocalIsSanction = customersResult[customerModel.CustomerId].FirstOrDefault(x => x.Name == CustomerProperty.Codes.localIsSanction.ToString())?.Value
                    });
                }
            }
            return queryItems;
        }

        private IDictionary<int, IList<CustomerPropertyModel>> GetCustomerDataForCustomerToScreen(IEnumerable<int> customerIdGroup, CustomerWriteRepository customerRepo, bool includeContactInfo)
        {
            var propertyNames = new HashSet<string>(new[]
                {
                    CustomerProperty.Codes.firstName.ToString(),
                    CustomerProperty.Codes.lastName.ToString(),
                    CustomerProperty.Codes.birthDate.ToString(),
                    CustomerProperty.Codes.civicRegNr.ToString(),
                    CustomerProperty.Codes.localIsPep.ToString(),
                    CustomerProperty.Codes.localIsSanction.ToString(),
                    CustomerProperty.Codes.email.ToString(),
                });
            if (includeContactInfo)
            {
                propertyNames.Add(CustomerProperty.Codes.addressStreet.ToString());
                propertyNames.Add(CustomerProperty.Codes.addressZipcode.ToString());
                propertyNames.Add(CustomerProperty.Codes.addressCity.ToString());
                propertyNames.Add(CustomerProperty.Codes.addressCountry.ToString());
            }
            return customerRepo.BulkFetch(new HashSet<int>(customerIdGroup), propertyNames: propertyNames);
        }

        private void ScreenQueryItems(List<QueryItemModel> queryItems, ICustomerContextExtended context, DateTime screenDate, CustomerWriteRepository customerRepo, Func<int, bool?> getLatestIsPepQuestionAnswer)
        {
            var kycScreeningService = kycServiceFactory.Value.CreateMultiCheckService();
            LogQueryItems(queryItems);
            var kycScreeningResult = kycScreeningService.Query(queryItems.Select(x => x.Item).ToList());

            var now = clock.Now;
            var itemsToEncrypt = new List<TrapetsQueryResultItem>();
            var newCustomerItems = new List<CustomerPropertyModel>();
            foreach (var q in queryItems)
            {
                var queryItem = q.Item;
                var hits = (kycScreeningResult.ContainsKey(queryItem.ItemId) ? kycScreeningResult[queryItem.ItemId] : null) ?? new List<KycScreeningListHit>();
                var cacheItem = context.FillInfrastructureFields(new TrapetsQueryResult
                {
                    CustomerId = q.CustomerId,
                    IsPepHit = hits.Any(x => x.IsPepHit),
                    IsSanctionHit = hits.Any(x => x.IsSanctionHit),
                    QueryDate = screenDate.Date
                });
                context.AddTrapetsQueryResults(cacheItem);

                void AddExternalIdsItemOnListHit(Func<KycScreeningListHit, bool> isHit, KycScreeningQueryResultItemCode itemCode)
                {
                    var externalIds = hits.Where(isHit).Select(x => x.ExternalId).ToList();
                    if (externalIds.Any())
                        AddScreeningItem(itemCode, JsonConvert.SerializeObject(externalIds), cacheItem, context);
                }

                AddExternalIdsItemOnListHit(x => x.IsPepHit, KycScreeningQueryResultItemCode.PepHitExternalIds);
                AddExternalIdsItemOnListHit(x => x.IsSanctionHit, KycScreeningQueryResultItemCode.SanctionHitExternalIds);

                var fullNameQuery = AddScreeningItem(KycScreeningQueryResultItemCode.QueryFullName, queryItem.FullName, cacheItem, context, isEncrypted: true);
                context.AddTrapetsQueryResultItems(fullNameQuery);
                itemsToEncrypt.Add(fullNameQuery);

                if (queryItem.TwoLetterIsoCountryCodes != null)
                {
                    AddScreeningItem(KycScreeningQueryResultItemCode.QueryCountryCodes, JsonConvert.SerializeObject(queryItem.TwoLetterIsoCountryCodes), cacheItem, context);
                }

                AddScreeningItem(KycScreeningQueryResultItemCode.QueryBirthDate, queryItem.BirthDate.ToString("yyyy-MM-dd"), cacheItem, context);
                AddScreeningItem(KycScreeningQueryResultItemCode.IsForDailyBatchScreen, "true", cacheItem, context);

                var newCustomerItemsToAdd = GetLocalPepAndSanctionStatusUpdateCustomerItems(q, cacheItem, getLatestIsPepQuestionAnswer);
                newCustomerItems.AddRange(newCustomerItemsToAdd);
            }

            encryptionService.SaveEncryptItems(itemsToEncrypt.ToArray(), x => x.Value, (x, y) => x.Value = y.ToString(), context);

            if (newCustomerItems.Any())
            {
                customerRepo.UpdateProperties(newCustomerItems, false);
            }
        }

        public static TrapetsQueryResultItem AddScreeningItem(KycScreeningQueryResultItemCode code, string value, TrapetsQueryResult result, ICustomerContextExtended context, bool isEncrypted = false)
        {
            var item = context.FillInfrastructureFields(new TrapetsQueryResultItem
            {
                IsEncrypted = isEncrypted,
                Name = code.ToString(),
                Value = value,
                QueryResult = result
            });
            context.AddTrapetsQueryResultItems(item);
            return item;
        }


        /// <summary>
        /// Used to allow verifying that contact info is included even when running against the mock service.
        /// </summary>
        private void LogQueryItems(List<QueryItemModel> queryItems)
        {
            if (string.IsNullOrWhiteSpace(envSettings.RelativeKycLogFolder))
                return;
            var absoluteLogFolder = Path.Combine(envSettings.LogFolder, envSettings.RelativeKycLogFolder);
            Directory.CreateDirectory(absoluteLogFolder);
            File.WriteAllText(Path.Combine(absoluteLogFolder, Guid.NewGuid().ToString() + ".txt"), JsonConvert.SerializeObject(queryItems, Formatting.Indented));
        }

        private bool? GetLatestIsPepQuestionAnswerDefault(int customerId)
        {
            var currentCustomerQuestions = kycManagementService.FetchLatestCustomerQuestionsSet(customerId);
            return currentCustomerQuestions?.Items
                ?.SingleOrDefault(x => x.QuestionCode.EqualsIgnoreCase("isPep"))?.AnswerCode.IsOneOfIgnoreCase("true", "yes");
        }

        private class QueryItemModel
        {
            public int CustomerId { get; set; }
            public KycScreeningQueryItem Item { get; set; }
            public string CurrentLocalIsPep { get; set; }
            public string CurrentLocalIsSanction { get; set; }
        }
    }
}