using Newtonsoft.Json;
using nPreCredit;
using NTech.Banking.CivicRegNumbers;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NTech.Core.PreCredit.Shared.Services
{
    public class BalanziaFiUnsecuredLoanApplicationCreationService
    {
        protected readonly ICoreClock clock;
        protected readonly IPublishEventService publishEventService;
        protected readonly IAdServiceIntegrationService adServiceIntegrationService;
        protected readonly IAbTestingService abTestingService;
        protected readonly IClientConfigurationCore clientConfiguration;
        protected readonly IPreCreditEnvSettings envSettings;
        protected readonly ICustomerClient customerClient;
        protected readonly UnsecuredCreditApplicationProviderRepository creditApplicationProviderRepository;
        protected readonly CachedSettingsService settingsService;

        public BalanziaFiUnsecuredLoanApplicationCreationService(ICoreClock clock,
            UnsecuredCreditApplicationProviderRepository creditApplicationProviderRepository,
            IPublishEventService publishEventService,
            IAdServiceIntegrationService adServiceIntegrationService,
            IAbTestingService abTestingService,
            IClientConfigurationCore clientConfiguration,
            IPreCreditEnvSettings envSettings,
            ICustomerClient customerClient,
            CachedSettingsService settingsService)
        {
            this.clock = clock;
            this.publishEventService = publishEventService;
            this.adServiceIntegrationService = adServiceIntegrationService;
            this.abTestingService = abTestingService;
            this.clientConfiguration = clientConfiguration;
            this.envSettings = envSettings;
            this.customerClient = customerClient;
            this.creditApplicationProviderRepository = creditApplicationProviderRepository;
            this.settingsService = settingsService;
        }

        public bool TryCreateBalanziaFiLikeApplication(LegacyUnsecuredLoanApplicationRequest request,
            bool? disableAutomation,
            bool? skipHideFromManualUserLists,
            INTechCurrentUserMetadata cu,
            out string failedMessage,
            out string applicationNr)
        {
            if (envSettings.IsMortgageLoansEnabled || envSettings.IsStandardUnsecuredLoansEnabled || envSettings.IsCompanyLoansEnabled)
                throw new Exception("Only supported for legacy unsecured loan applications");

            if (cu.IsProvider)
            {
                if (envSettings.IsProduction)
                    disableAutomation = null; //only system users are allowed to opt out of automation in production

                var providerName = cu.ProviderName;
                if (providerName == null)
                    throw new Exception("Missing providername on user");
                if (request.ProviderName == null)
                    request.ProviderName = providerName;
                else if (request.ProviderName != providerName)
                    throw new Exception("Providers can only add applications for themselves");
            }
            else if (!cu.IsSystemUser)
            {
                throw new NotImplementedException();
            }

            if (request.ProviderName == null)
                throw new Exception("Missing provider");

            var affiliate = envSettings.GetAffiliateModel(request.ProviderName);

            return TryCreateBalanziaFiLikeApplicationInternal(request, disableAutomation, skipHideFromManualUserLists, cu, affiliate, out failedMessage, out applicationNr);
        }

        protected bool TryCreateBalanziaFiLikeApplicationInternal(
            LegacyUnsecuredLoanApplicationRequest request,
        bool? disableAutomation,
        bool? skipHideFromManualUserLists,
        INTechCurrentUserMetadata cu,
        AffiliateModel affiliate,
        out string failedMessage,
        out string applicationNr)
        {
            List<string> errors = new List<string>();

            var internalRequest = TranslateCreditApplicationRequest(request, affiliate, errors, disableAutomation ?? false, skipHideFromManualUserLists ?? false, cu);

            if (errors.Count > 0)
            {
                applicationNr = null;
                failedMessage = string.Join(", ", errors);
                return false;
            }

            applicationNr = creditApplicationProviderRepository.CreateCreditApplication(internalRequest);

            publishEventService.Publish(PreCreditEventCode.CreditApplicationCreated, JsonConvert.SerializeObject(new { applicationNr = applicationNr, disableAutomation = disableAutomation }));

            failedMessage = null;
            return true;
        }

        private TranslatedLegacyUnsecuredLoanApplicationRequest TranslateCreditApplicationRequest(
            LegacyUnsecuredLoanApplicationRequest request, AffiliateModel affiliate, List<string> errors, bool disableAutomation, bool skipHideFromManualUserLists,
            INTechCurrentUserMetadata ntechCurrentUserMetadata)
        {
            var additionalCommentParts = new List<string>();

            if (request.Items == null)
            {
                request.Items = new LegacyUnsecuredLoanApplicationRequest.Item[] { };
            }
            request.Items = FilterProviderBannedItems(request.Items);

            var wrappedItems = (request.Items ?? new LegacyUnsecuredLoanApplicationRequest.Item[] { })
                .Select(x => new ItemWrapper
                {
                    Item = x,
                    Handled = false
                })
                .ToList();

            Func<string, string, bool> eq = (s1, s2) => s1.Equals(s2, StringComparison.InvariantCultureIgnoreCase);
            Func<string, string, bool> endsWith = (s1, s2) => s1.ToLowerInvariant().EndsWith(s2.ToLowerInvariant());
            Func<string, string, bool> startsWith = (s1, s2) => s1.ToLowerInvariant().StartsWith(s2.ToLowerInvariant());
            Action<Func<string, string, bool>, Action<List<LegacyUnsecuredLoanApplicationRequest.Item>>> handleByPredicate = (keyPredicate, a) =>
            {
                var hits = wrappedItems.Where(x => keyPredicate(x.Item.Group, x.Item.Name) && !x.Handled).ToList();
                a(hits.Select(x => x.Item).ToList());
                hits.ForEach(x => x.Handled = true);
            };
            Func<List<LegacyUnsecuredLoanApplicationRequest.Item>, string, string, string> singleOrError = (items, group, name) =>
            {
                var hits = items.Where(x => eq(x.Group, group) && eq(x.Name, name)).ToList();
                if (hits.Count > 1)
                {
                    errors.Add($"{group}.{name} is included more than once");
                    return null;
                }
                if (hits.Count == 0)
                {
                    errors.Add($"{group}.{name} is missing");
                    return null;
                }
                return hits[0].Value;
            };
            var now = this.clock.Now;
            var experiment = abTestingService.AssignExperimentOrNull();
            var ireq = new TranslatedLegacyUnsecuredLoanApplicationRequest
            {
                ProviderName = affiliate.ProviderName,
                CreatedById = ntechCurrentUserMetadata.UserId,
                CreationDate = now,
                ApplicationDate = now,
                NrOfApplicants = request.NrOfApplicants,
                InformationMetadata = ntechCurrentUserMetadata.InformationMetadata,
                Items = new List<TranslatedLegacyUnsecuredLoanApplicationRequest.CreditApplicationItem>(),
                SearchTerms = new List<TranslatedLegacyUnsecuredLoanApplicationRequest.ApplicationSearchTerm>(),
                DisableAutomation = disableAutomation,
                SkipHideFromManualUserLists = skipHideFromManualUserLists,
                ApplicationType = CreditApplicationTypeCode.unsecuredLoan.ToString(),
                RequestIpAddress = request?.RequestIpAddress,
                AbTestExperiment = experiment == null ? null : new TranslatedLegacyUnsecuredLoanApplicationRequest.AbTestExperimentModel
                {
                    ExperimentId = experiment.ExperimentId,
                    VariationName = experiment.VariationName
                }
            };
            if (experiment != null)
            {

                additionalCommentParts.Add($"Part of A/B test '{experiment.ExperimentName}' ({experiment.ExperimentId}) in the {(experiment.HasVariation ? "B" : "A")} group");
            }

            var civicRegNrDupePreventionSet = new HashSet<string>();

            Action<string, string, string, bool> addItem = (group, name, value, isSensitive) => ireq.Items.Add(new TranslatedLegacyUnsecuredLoanApplicationRequest.CreditApplicationItem
            {
                GroupName = group,
                Name = name,
                Value = value,
                IsSensitive = isSensitive
            });

            var scoringVersion = envSettings.DefaultScoringVersion;
            if (!string.IsNullOrWhiteSpace(scoringVersion))
            {
                addItem("application", "scoringVersion", scoringVersion, false);
            }

            var psd2Settings = settingsService.LoadSettings("psd2Settings");
            var isForcedBankAccountDataSharing = psd2Settings.Opt("isForcedBankAccountDataSharingEnabled") == "true";
            if (isForcedBankAccountDataSharing)
            {
                addItem("application", "IsForcedBankAccountDataSharing", isForcedBankAccountDataSharing.ToString(), false);
            }

            //Broken items
            handleByPredicate((group, name) => string.IsNullOrWhiteSpace(group) || string.IsNullOrWhiteSpace(name), items =>
            {
                if (items.Count > 0)
                    errors.Add($"{items.Count} items are missing group and/or name");
            });

            //Group not allowed
            handleByPredicate((group, name) => !(eq(group, "application") || startsWith(group, "applicant") || startsWith(group, "question")), items =>
            {
                if (items.Count > 0)
                    errors.Add($"Not allowed groups: [{string.Join(", ", items.Select(x => x.Group).Distinct())}]");
            });

            //Trim everything
            wrappedItems.Where(x => !x.Handled).ToList().ForEach(x =>
            {
                x.Item.Group = x.Item.Group.Trim();
                x.Item.Name = x.Item.Name.Trim();
                x.Item.Value = x.Item.Value?.Trim();
            });

            //Nr of applicants must be included
            if (request.NrOfApplicants < 1)
            {
                errors.Add("NrOfApplicants must be 1 or more");
            }

            //Check for nr of applicants mismatch
            var actualApplicantGroupNames = wrappedItems.Where(x => !x.Handled && x.Item.Group.StartsWith("applicant")).Select(x => x.Item.Group).Distinct();
            var expectedApplicantGroupNames = Enumerable.Range(1, request.NrOfApplicants).Select(x => $"applicant{x}").ToList();
            if (actualApplicantGroupNames.Any(x => !expectedApplicantGroupNames.Contains(x)))
            {
                errors.Add("NrOfApplicants and actual included groups named applicant<i> do not match");
            }

            //TODO: AgreementSigningProvider.UpdateCustomerWithQuestionsFromApplication(.... merge with the below)
            //Per applicant
            for (var i = 1; i <= request.NrOfApplicants; i++)
            {
                var group = $"applicant{i}";

                //CivicRegNr
                handleByPredicate((g, n) => g == group && (n == "civicRegNr" || n == "civicRegNrCountry"), items =>
                {
                    var civicRegNr = singleOrError(items, group, "civicRegNr");
                    var civicRegNrCountry = singleOrError(items, group, "civicRegNrCountry");

                    if (civicRegNr != null && civicRegNrCountry != null)
                    {
                        if (eq(civicRegNrCountry, clientConfiguration.Country.BaseCountry))
                        {
                            ICivicRegNumber c;
                            if (new CivicRegNumberParser(clientConfiguration.Country.BaseCountry).TryParse(civicRegNr, out c))
                            {
                                if (civicRegNrDupePreventionSet.Contains(c.NormalizedValue))
                                {
                                    errors.Add($"{group}.civicRegNr is the same as another applicants.");
                                }
                                else
                                {
                                    addItem(group, "civicRegNr", c.NormalizedValue, true);
                                    addItem(group, "birthDate", c.BirthDate.Value.ToString("yyyy-MM-dd"), false);
                                    var customerId = customerClient.GetCustomerId(c);
                                    addItem(group, "customerId", customerId.ToString(), false);
                                    civicRegNrDupePreventionSet.Add(c.NormalizedValue);
                                }
                            }
                            else
                            {
                                errors.Add($"{group}.civicRegNr failed validation for country {civicRegNrCountry}.");
                            }
                        }
                        else
                        {
                            errors.Add($"civicRegNrCountry = {civicRegNrCountry} is not supported");
                        }
                    }
                });
            }

            //Must be a decimal
            handleByPredicate((g, n) => endsWith(n, "amount"), items =>
            {
                foreach (var item in items)
                {
                    decimal d;
                    if (decimal.TryParse(string.IsNullOrWhiteSpace(item.Value) ? "0" : item.Value.Trim(),
                        NumberStyles.Integer | NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture,
                        out d))
                    {
                        addItem(item.Group, item.Name, d.ToString(CultureInfo.InvariantCulture), false);
                    }
                    else
                    {
                        errors.Add($"Item {item.Group}.{item.Name} must be a valid decimal if included. Format like 999.99");
                    }
                }
            });

            //Ints
            handleByPredicate((g, n) => startsWith(n, "repaymenttime"), items =>
            {
                foreach (var item in items)
                {
                    int d;
                    if (int.TryParse(string.IsNullOrWhiteSpace(item.Value) ? "0" : item.Value.Trim(), out d))
                    {
                        addItem(item.Group, item.Name, d.ToString(CultureInfo.InvariantCulture), false);
                    }
                    else
                    {
                        errors.Add($"Item {item.Group}.{item.Name} must be a valid integer if included. Format like 999");
                    }
                }
            });

            //Last step, check for required
            handleByPredicate((g, n) => true, items =>
            {
                foreach (var item in items)
                    addItem(item.Group, item.Name, item.Value, false);
            });

            //Check for missing required items without special rules
            foreach (var item in GetRequiredItems(request.NrOfApplicants))
            {
                if (!ireq.Items.Any(x => eq(x.GroupName, item.Item1) && eq(x.Name, item.Item2)))
                {
                    errors.Add($"Missing required item {item.Item1}.{item.Item2}");
                }
            }

            //Use fallback campaign code if none is included
            if (!ireq.Items.Any(x => x.GroupName == "application" && x.Name == "campaignCode") && !string.IsNullOrWhiteSpace(affiliate.FallbackCampaignCode))
            {
                addItem("application", "campaignCode", affiliate.FallbackCampaignCode, false);
            }

            var storedExternalVariables = new Dictionary<string, string>();
            var isFromAdServices = false;
            if (request.ExternalVariables != null)
            {
                foreach (var e in request.ExternalVariables)
                {
                    if (this.adServiceIntegrationService.GetUsedExternalVariableNames().Contains(e.Name))
                    {
                        storedExternalVariables[e.Name] = e.Value;
                        isFromAdServices = true;
                    }
                }
            }

            if (isFromAdServices)
            {
                var adServicesVariables = storedExternalVariables.Where(x => this.adServiceIntegrationService.GetUsedExternalVariableNames().Contains(x.Key)).ToList();
                additionalCommentParts.Add($"Sent by ad services (" + string.Join(", ", adServicesVariables.Select(x => $"{x.Key}={x.Value}")) + ")");
            }

            foreach (var s in storedExternalVariables)
                addItem("external", s.Key, s.Value, false);

            ireq.CreationMessage = "Application created" + (additionalCommentParts.Any() ? (". " + string.Join(". ", additionalCommentParts)) : "");

            return ireq;
        }

        private LegacyUnsecuredLoanApplicationRequest.Item[] FilterProviderBannedItems(LegacyUnsecuredLoanApplicationRequest.Item[] items)
        {
            Func<LegacyUnsecuredLoanApplicationRequest.Item, string, string, bool> f = (i, gn, nn) => i.Group.StartsWith(gn, StringComparison.InvariantCultureIgnoreCase) && i.Name == nn;
            return items.Where(x =>
                !f(x, "affiliate_code", "creditnr") &&
                !f(x, "application", "creditnr") &&
                !f(x, "applicant", "customerId") &&
                !f(x, "application", "scoringVersion")
            ).ToArray();
        }

        private IEnumerable<Tuple<string, string>> GetRequiredItems(int nrOfApplicants)
        {
            yield return Tuple.Create("application", "amount");
        }

        private class ItemWrapper
        {
            public LegacyUnsecuredLoanApplicationRequest.Item Item { get; set; }
            public bool Handled { get; set; }
        }

    }

    public class LegacyUnsecuredLoanApplicationRequest
    {
        public string ProviderName { get; set; }
        public string RequestIpAddress { get; set; }
        public int NrOfApplicants { get; set; }
        public Item[] Items { get; set; }
        public string ApplicationRequestJson { get; set; }
        public class Item
        {
            public string Group { get; set; }
            public string Name { get; set; }
            public string Value { get; set; }
        }
        public List<ExternalVariableItem> ExternalVariables { get; set; }
        public class ExternalVariableItem
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }
    }

    public class TranslatedLegacyUnsecuredLoanApplicationRequest
    {
        public string ProviderName { get; set; }
        public int NrOfApplicants { get; set; }
        public int CreatedById { get; set; }
        public string RequestIpAddress { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public DateTimeOffset ApplicationDate { get; set; }
        public string InformationMetadata { get; set; }
        public string CreationMessage { get; set; }
        public List<CreditApplicationItem> Items { get; set; }
        public List<ApplicationSearchTerm> SearchTerms { get; set; }
        public List<CustomerClientCustomerPropertyModel> InitialCustomerUpdateItems { get; set; }
        public bool DisableAutomation { get; set; }
        public bool SkipHideFromManualUserLists { get; set; }
        public string ApplicationType { get; set; }
        public string ApplicationRequestJson { get; set; }
        public AbTestExperimentModel AbTestExperiment { get; set; }

        public class AbTestExperimentModel
        {
            public string VariationName { get; set; }
            public int ExperimentId { get; set; }
        }

        public class ApplicationSearchTerm
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        public class CreditApplicationItem
        {
            public string GroupName { get; set; }
            public string Name { get; set; }
            public string Value { get; set; }
            public bool IsSensitive { get; set; } //Will cause encryption
        }
    }

    public interface IAbTestingService
    {
        ApplicationAbTestExperimentModel AssignExperimentOrNull();
        ITestingVariationSet GetVariationSetForApplication(string applicationNr);
    }

    public class ApplicationAbTestExperimentModel
    {
        public int ExperimentId { get; set; }
        public string ExperimentName { get; set; }
        public bool HasVariation { get; set; }
        public string VariationName { get; set; }

        public ITestingVariationSet GetVariationSet()
        {
            return new SingleItemVariationSet { VariationName = HasVariation ? VariationName : null };
        }

        private class SingleItemVariationSet : ITestingVariationSet
        {
            public string VariationName { get; set; }

            public bool HasVariation(string name)
            {
                return !string.IsNullOrWhiteSpace(VariationName) && VariationName.EqualsIgnoreCase(name);
            }
        }
    }

    public class EmptyVariationSet : ITestingVariationSet
    {
        public bool HasVariation(string name) => false;
    }

    public interface ITestingVariationSet
    {
        bool HasVariation(string name);
    }

    public interface IAdServiceIntegrationService
    {
        ISet<string> GetUsedExternalVariableNames();
        bool IsEnabled { get; }
        void ReportConversion(IDictionary<string, string> externalVariables, string orderId, string priceVariable);
    }

    public interface IPublishEventService
    {
        void Publish(string eventCode, string data);
        void Publish(PreCreditEventCode eventCode, string data);
    }
}
