using nCustomer.Code.Services.Settings;
using nCustomer.DbModel;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure.Email;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCustomer.Code.Services
{
    public class CustomerMessageService : NonSearchCustomerMessageService, ICustomerMessageService
    {
        private readonly Func<CustomersContext, NtechCurrentUserMetadata, CustomerSearchRepository> createCustomerRepository;
        private readonly Lazy<ICompanyLoanNameSearchService> companyLoanNameSearchService;

        public CustomerMessageService(
            Func<CustomersContext> createContext,
            Func<CustomersContext, NtechCurrentUserMetadata, CustomerSearchRepository> createCustomerRepository,
            Lazy<ICompanyLoanNameSearchService> companyLoanNameSearchService,
            INTechEmailServiceFactory emailServiceFactory,
            IKeyValueStoreService keyValueStoreService,
            ReadonlySettingsService settingsService,
            IClientConfigurationCore clientConfiguration,
            ILoggingService loggingService,
            INTechServiceRegistry serviceRegistry,
            EncryptionService encryptionService) : base(createContext, encryptionService, emailServiceFactory, keyValueStoreService,
                settingsService, clientConfiguration, loggingService, serviceRegistry, NEnv.IsStandardUnsecuredLoansEnabled,
                new NTech.Core.Customer.Shared.Services.CrossModuleClientFactory(
                    new Lazy<ICreditClient>(() => LegacyServiceClientFactory.CreateCreditClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry)),
                    new Lazy<ISavingsClient>(() => LegacyServiceClientFactory.CreateSavingsClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry)),
                    new Lazy<IPreCreditClient>(() => LegacyServiceClientFactory.CreatePreCreditClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry))),
                CoreClock.SharedInstance,
                EventSubscriberBase.SharedService)
        {
            this.createCustomerRepository = createCustomerRepository;
            this.companyLoanNameSearchService = companyLoanNameSearchService;
        }

        private bool TryAddCustomerIdsByPossibleEmail(string emailCandidate, ISet<int> customerIds, CustomerSearchRepository customerRepository)
        {
            if (!emailCandidate.Contains("@") || emailCandidate.Contains(" "))
                return false;

            customerIds.AddRange(customerRepository.FindCustomersMatchingAllSearchTerms(
                new Tuple<string, string>[] { Tuple.Create(SearchTermCode.email.ToString(), emailCandidate) }));

            return true;
        }

        private bool TryAddCustomerIdsByPossibleCivicRegNrOrOrgNr(string nr, ISet<int> customerIds, CustomersContext context)
        {
            var wasAnyValid = false;
            if (NEnv.BaseCivicRegNumberParser.TryParse(nr, out var civicRegNr))
            {
                customerIds.Add(CustomerIdSource.GetCustomerIdByCivicRegNr(civicRegNr, context));
                wasAnyValid = true;
            }
            if (NEnv.BaseOrganisationNumberParser.TryParse(nr, out var orgnr))
            {
                customerIds.Add(CustomerIdSource.GetCustomerIdByOrgnr(orgnr, context));
                wasAnyValid = true;
            }
            return wasAnyValid;
        }

        private void AddCustomerIdsByCustomerName(string name, ISet<int> customerIds, CustomerSearchRepository customerRepository)
        {
            customerIds.AddRange(customerRepository.FindCustomersByName(name));
            if (NEnv.IsCompanyLoansEnabled)
                customerIds.AddRange(companyLoanNameSearchService.Value.FindCustomerByCompanyName(name));
        }

        private int AddCustomerIdsByRelationId(string name, ISet<int> customerIds, CustomersContext context)
        {
            var hits = GetCustomerRelationChannelsQueryable(context)
                .Where(x => x.ChannelId == name)
                .Select(x => x.CustomerId)
                .ToList();

            customerIds.AddRange(hits);

            return hits.Count;
        }

        private void AddCustomerIdsByOmniValue(string omniText, ISet<int> customerIds, CustomersContext context, CustomerSearchRepository customerRepository)
        {
            if (TryAddCustomerIdsByPossibleEmail(omniText, customerIds, customerRepository))
                return;
            if (TryAddCustomerIdsByPossibleCivicRegNrOrOrgNr(omniText, customerIds, context))
                return;
            if (AddCustomerIdsByRelationId(omniText, customerIds, context) > 0)
                return;
            AddCustomerIdsByCustomerName(omniText, customerIds, customerRepository);
        }

        public List<MessageChannelModel> FindChannels(CustomerMessageChannelSearchTypeCode searchType, string searchText, NtechCurrentUserMetadata currentUser, bool includeGeneralChannels)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return new List<MessageChannelModel>();

            searchText = searchText.Trim();

            using (var context = new CustomersContext())
            {
                var customerRepository = createCustomerRepository(context, currentUser);

                var customerIds = new HashSet<int>();

                if (searchType == CustomerMessageChannelSearchTypeCode.Omni)
                    AddCustomerIdsByOmniValue(searchText, customerIds, context, customerRepository);
                else if (searchType == CustomerMessageChannelSearchTypeCode.Email)
                    TryAddCustomerIdsByPossibleEmail(searchText, customerIds, customerRepository);
                else if (searchType == CustomerMessageChannelSearchTypeCode.OrgOrCivicRegNr)
                    TryAddCustomerIdsByPossibleCivicRegNrOrOrgNr(searchText, customerIds, context);
                else if (searchType == CustomerMessageChannelSearchTypeCode.RelationId)
                    AddCustomerIdsByRelationId(searchText, customerIds, context);
                else if (searchType == CustomerMessageChannelSearchTypeCode.CustomerName)
                    AddCustomerIdsByCustomerName(searchText, customerIds, customerRepository);

                if (!customerIds.Any())
                    return new List<MessageChannelModel>();

                var channels = new List<MessageChannelModel>();
                channels.AddRange(GetCustomerRelationChannelsQueryable(context)
                        .Where(x => customerIds.Contains(x.CustomerId))
                        .ToList());

                if (searchType == CustomerMessageChannelSearchTypeCode.Omni || searchType == CustomerMessageChannelSearchTypeCode.RelationId)
                {
                    //If any channelid exactly matches the search term show only that
                    var matchedChannels = channels.Where(x => x.IsRelation && x.ChannelId == searchText).ToList();
                    if (matchedChannels.Count > 0)
                        return matchedChannels;
                }

                if (includeGeneralChannels)
                {
                    //Add a general channel for each customer, even ones who have no relations (this last bit is a bit debatable)
                    return channels
                        .Concat(customerIds
                            .Select(x => new MessageChannelModel
                            {
                                ChannelId = GeneralChannelId,
                                ChannelType = GeneralChannelType,
                                IsRelation = false,
                                CustomerId = x
                            })).ToList();
                }

                return channels;
            }
        }
    }
}