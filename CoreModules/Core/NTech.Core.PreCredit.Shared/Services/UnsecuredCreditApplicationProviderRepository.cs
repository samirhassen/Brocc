using nPreCredit;
using nPreCredit.Code.Services;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Core.PreCredit.Shared.Services
{
    public class UnsecuredCreditApplicationProviderRepository
    {
        private readonly IPreCreditEnvSettings envSettings;
        private readonly ICoreClock coreClock;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly ICustomerClient customerClient;
        private readonly Func<ILegacyUnsecuredCreditApplicationDbWriter> createWriter;
        private readonly ICreditApplicationKeySequenceGenerator applicationKeySequenceGenerator;

        public UnsecuredCreditApplicationProviderRepository(
            IPreCreditEnvSettings envSettings, ICoreClock coreClock,
            IClientConfigurationCore clientConfiguration, ICustomerClient customerClient,
            Func<ILegacyUnsecuredCreditApplicationDbWriter> createWriter,
            ICreditApplicationKeySequenceGenerator applicationKeySequenceGenerator)
        {
            this.envSettings = envSettings;
            this.coreClock = coreClock;
            this.clientConfiguration = clientConfiguration;
            this.customerClient = customerClient;
            this.createWriter = createWriter;
            this.applicationKeySequenceGenerator = applicationKeySequenceGenerator;
        }

        public string CreateCreditApplication(TranslatedLegacyUnsecuredLoanApplicationRequest request)
        {
            // Balanzia fi uses this code path
            if (!envSettings.IsUnsecuredLoansEnabled || envSettings.IsStandardUnsecuredLoansEnabled)
                throw new NotImplementedException();

            if (request == null)
                throw new ArgumentNullException("request");

            var customerUpdateItems = new List<CustomerClientCustomerPropertyModel>();
            var requestItems = request.Items;

            using (var writer = createWriter())
            {
                var nr = new CreditApplicationNrGenerator(() => "CA", applicationKeySequenceGenerator).GenerateNewApplicationNr();
                var header = new
                {
                    Header = CreditApplicationHeader(request, requestItems, nr)
                };

                var h = header;
                if (request.InitialCustomerUpdateItems != null)
                    customerUpdateItems.AddRange(request.InitialCustomerUpdateItems);

                foreach (var applicantNr in Enumerable.Range(1, h.Header.NrOfApplicants))
                {
                    var applicantItems = h.Header.Items.Where(x => x.GroupName == ("applicant" + applicantNr)).ToList();

                    var customerId = int.Parse(applicantItems.Single(x => x.Name == "customerId").Value);

                    customerUpdateItems.AddRange(applicantItems
                            .Where(x => new List<string> { "civicRegNr", "civicRegNrCountry" }.Contains(x.Name))
                            .Select(x => new CustomerClientCustomerPropertyModel
                            {
                                CustomerId = customerId,
                                Name = x.Name,
                                Group = "civicRegNr",
                                Value = x.Value,
                                IsSensitive = true,
                            }).ToList());

                    customerUpdateItems.AddRange(applicantItems
                            .Where(x => new List<string> { "email", "phone" }.Contains(x.Name))
                            .Select(x => new CustomerClientCustomerPropertyModel
                            {
                                CustomerId = customerId,
                                Name = x.Name,
                                Group = "insensitive",
                                Value = x.Value,
                                IsSensitive = false,
                            }).ToList());
                }

                if (customerUpdateItems.Any())
                    customerClient.UpdateCustomerCard(customerUpdateItems, false);

                if (request?.ApplicationRequestJson != null)
                {
                    writer.StoreApplicationRequestJson(header.Header.ApplicationNr, request?.ApplicationRequestJson);
                }

                //Encrypted items
                var itemsToEncrypt = header
                    .Header
                    .Items
                    .Where(x => x.IsEncrypted)
                    .ToArray();

                //Set the value of encrypted items to the id in EncryptedValue
                writer.SaveEncryptItems(
                    itemsToEncrypt,
                    x => x.Value,
                    (x, id) => x.Value = id.ToString());

                var attachment = request.RequestIpAddress == null ? null : CommentAttachment.CreateMetadataOnly(requestIpAddress: request.RequestIpAddress);
                writer.AddMetadataOnlyComment(nr, request.CreationMessage ?? "Application created", "ApplicationCreated", attachment);

                writer.AddCreditApplicationHeader(header.Header);
                writer.AddCreditApplicationItems(header.Header.Items);
                writer.AddCreditApplicationSearchTerms(header.Header.SearchTerms);
                var evt = writer.CreateAndAddApplicationEvent(CreditApplicationEventCode.CreditApplicationCreated);
                evt.Application = header.Header;

                if (request.AbTestExperiment != null)
                {
                    var e = request.AbTestExperiment;
                    var hasVariation = !string.IsNullOrWhiteSpace(e.VariationName);
                    var data = new Dictionary<string, string>
                    {
                        {"ExperimentId", e.ExperimentId.ToString()},
                        {"HasVariation", hasVariation ? "true" : "false"},
                    };
                    if (hasVariation)
                        data["VariationName"] = e.VariationName.Trim();

                    writer.SetUniqueComplexApplicationListItems(
                        header.Header.ApplicationNr, "AbTestingExperiment", 1,
                        data);
                }

                writer.SaveChanges();
                writer.Commit();

                return nr;
            }
        }


        protected CreditApplicationHeader CreditApplicationHeader(
            TranslatedLegacyUnsecuredLoanApplicationRequest request,
            List<TranslatedLegacyUnsecuredLoanApplicationRequest.CreditApplicationItem> requestItems,
            string applicationNr)
        {
            return new CreditApplicationHeader
            {
                ProviderName = request.ProviderName,
                ApplicationNr = applicationNr,
                ApplicationType = request.ApplicationType,
                NrOfApplicants = request.NrOfApplicants,
                ChangedById = request.CreatedById,
                ChangedDate = request.CreationDate,
                ApplicationDate = request.ApplicationDate,
                IsActive = true,
                AgreementStatus = CreditApplicationMarkerStatusName.Initial,
                CreditCheckStatus = CreditApplicationMarkerStatusName.Initial,
                FraudCheckStatus = CreditApplicationMarkerStatusName.Initial,
                CustomerCheckStatus = CreditApplicationMarkerStatusName.Initial,
                IsFinalDecisionMade = false,
                InformationMetaData = request.InformationMetadata,
                Items = requestItems.Select(x => new CreditApplicationItem
                {
                    ApplicationNr = applicationNr,
                    Name = x.Name,
                    GroupName = x.GroupName,
                    Value = x.Value,
                    IsEncrypted = x.IsSensitive,
                    ChangedById = request.CreatedById,
                    ChangedDate = request.CreationDate,
                    AddedInStepName = "Initial",
                    InformationMetaData = request.InformationMetadata
                }).ToList(),
                SearchTerms = request.SearchTerms.Select(x => new CreditApplicationSearchTerm
                {
                    ApplicationNr = applicationNr,
                    ChangedById = request.CreatedById,
                    ChangedDate = request.CreationDate,
                    Name = x.Name,
                    Value = x.Value,
                    InformationMetaData = request.InformationMetadata,
                }).ToList(),
                HideFromManualListsUntilDate = (request.SkipHideFromManualUserLists || request.DisableAutomation || envSettings.CreditApplicationWorkListIsNewMinutes <= 0)
                        ? null
                        : new DateTimeOffset?(coreClock.Now.AddMinutes(envSettings.CreditApplicationWorkListIsNewMinutes)),
                CanSkipAdditionalQuestions = false
            };
        }


        /// <param name="applicationItems">List((group name, item name)</param>
        public static bool ContainsEnoughFieldsToSkipAdditionalQuestions(int nrOfApplicants, IEnumerable<Tuple<string, string>> applicationItems, string clientCountry)
        {
            if (applicationItems == null)
                return false;
            Func<string, string, bool> isPresent = (g, n) =>
                applicationItems.Any(x =>
                    x.Item1.Equals(g, StringComparison.InvariantCultureIgnoreCase)
                    && x.Item2.Equals(n, StringComparison.InvariantCultureIgnoreCase));

            Func<string, bool> isQuestionPresentForAllApplicants = questionName =>
            {
                foreach (var g in Enumerable.Range(1, nrOfApplicants).Select(x => $"question{x}"))
                {
                    if (!isPresent(g, questionName))
                        return false;
                }
                return true;
            };

            if (!isQuestionPresentForAllApplicants("loan_purpose"))
                return false;
            if (!isQuestionPresentForAllApplicants("loan_whosmoney"))
                return false;
            if (!isQuestionPresentForAllApplicants("customer_ispep"))
                return false;
            if (!isQuestionPresentForAllApplicants("customer_taxcountries"))
                return false;

            if (clientCountry == "FI")
            {
                if (!isPresent("application", "iban"))
                    return false;
            }
            else if (clientCountry == "SE")
            {
                if (!isPresent("application", "bankaccountnr"))
                    return false;
            }
            else
                throw new NotImplementedException();

            return true;
        }
    }
}
