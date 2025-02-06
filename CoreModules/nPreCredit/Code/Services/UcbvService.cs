using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace nPreCredit.Code.Services
{
    public class UcbvService : IUcbvService
    {
        private readonly IUcBvCreditReportClient creditReportClient;
        private readonly IMortgageLoanObjectService mortgageLoanObjectService;
        private readonly IMortgageLoanWorkflowService mortgageLoanWorkflowService;
        private readonly KeyValueStore ucbvStore;

        private readonly UpdateCreditApplicationRepository updateCreditApplicationRepository;

        public static string SerializeValuationResult(Dictionary<string, string> ucbvResult)
        {
            return JsonConvert.SerializeObject(new { items = ucbvResult });
        }

        public static Dictionary<string, string> DeserializeValuationResult(string value)
        {
            return JsonConvert.DeserializeAnonymousType(value, new { items = (Dictionary<string, string>)null })?.items;
        }

        public static Dictionary<string, string> GetValuationResultFromStore(IKeyValueStoreService store, string valuationId)
        {
            return DeserializeValuationResult(store.GetValue(valuationId, KeyValueStoreKeySpaceCode.UcbvValuationV1.ToString()));
        }

        public UcbvService(IUcBvCreditReportClient creditReportClient, IKeyValueStoreService keyValueStoreService, UpdateCreditApplicationRepository updateCreditApplicationRepository, IMortgageLoanObjectService mortgageLoanObjectService, IMortgageLoanWorkflowService mortgageLoanWorkflowService)
        {
            this.creditReportClient = creditReportClient;
            this.mortgageLoanObjectService = mortgageLoanObjectService;
            this.mortgageLoanWorkflowService = mortgageLoanWorkflowService;
            this.updateCreditApplicationRepository = updateCreditApplicationRepository;
            this.ucbvStore = new KeyValueStore(KeyValueStoreKeySpaceCode.UcbvValuationV1, keyValueStoreService);
        }

        public bool TryAcceptValuation(string applicationNr, Dictionary<string, string> ucbvResult, out string failedMessage, out string valuationId)
        {
            //See MortageLoan-Securities-SupportedFields.xlsx for allowed fields in ucbvResult
            var key = Guid.NewGuid().ToString();
            ucbvStore.SetValue(key, SerializeValuationResult(ucbvResult));

            updateCreditApplicationRepository.UpdateApplication(applicationNr, new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest
            {
                StepName = "AcceptUcbvValuation",
                Items = new List<UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem>
                {
                    new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem
                    {
                        GroupName = "application",
                        Name = ApplicationInfoService.UcbvServiceCreditApplicationItemName,
                        Value = key
                    }
                }
            }, also: context =>
            {
                mortgageLoanWorkflowService.ChangeStepStatusComposable(context, MortgageLoanApplicationValuationService.WorkflowStepName, mortgageLoanWorkflowService.AcceptedStatusName, applicationNr: applicationNr);
            });

            failedMessage = null;
            valuationId = key;
            return true;
        }

        public bool TryAutomateValution(string applicationNr, out string failedMessage, out Dictionary<string, string> ucbvResult)
        {
            //TODO: Needs a cross client model for current loans or some other way of getting RequestedAmortizationAmount
            throw new NotImplementedException();

            //ucbvResult = null;

            //var objectInfo = mortgageLoanObjectService.GetObject(applicationNr);
            //if(objectInfo == null)
            //{
            //    failedMessage = "Missing object information";
            //    return false;
            //}

            //var propertyDetails = objectInfo.CondominiumPropertyDetails;
            //if(propertyDetails == null)
            //{
            //    failedMessage = "Missing object information details or objects in not an apartment";
            //    return false;
            //}

            //if(!propertyDetails.ApartmentNumber.HasValue)
            //{
            //    failedMessage = "Missing apartment nr";
            //    return false;
            //}

            //var lghNr = propertyDetails.ApartmentNumber.Value.ToString();

            //if (string.IsNullOrWhiteSpace(lghNr))
            //{
            //    failedMessage = "Customer street adress is missing lägenhetsnummer";
            //    return false;
            //}

            //var adrResult = creditReportClient.UcbvSokAddress(propertyDetails.Address, propertyDetails.PostalCode?.ToString(), propertyDetails.City, objectInfo.PropertyMunicipality);

            //if (!adrResult.Item1)
            //{
            //    failedMessage = $"UcbvSokAddress failed: {adrResult.Item3}";
            //    return false;
            //}

            //var addressHits = adrResult.Item2;
            //if (addressHits.Count == 0)
            //{
            //    failedMessage = "UcbvSokAddress got not hits";
            //    return false;
            //}

            //if (addressHits.Count > 1)
            //{
            //    failedMessage = $"UcbvSokAddress got {addressHits.Count} hits";
            //    return false;
            //}

            //var adrHit = addressHits.Single();

            //var objectId = adrHit.Id;

            //var objectResult = creditReportClient.UcbvHamtaObjekt(objectId);

            //if (!objectResult.Item1)
            //{
            //    failedMessage = $"UcbvHamtaObjekt failed: {objectResult.Item3}";
            //    return false;
            //}

            //var objHit = objectResult.Item2;
            //if (objHit == null)
            //{
            //    failedMessage = $"UcbvHamtaObjekt got no hit";
            //    return false;
            //}

            //var lgh = objHit.Lagenheter?.Where(x => x.Lghnr == lghNr)?.ToList();

            //if (lgh == null || lgh.Count == 0)
            //{
            //    failedMessage = $"UcbvHamtaObjekt got a hit but the lägenhetsnummer {lghNr} does not exist in the object";
            //    return false;
            //}

            //if (lgh != null && lgh.Count > 1)
            //{
            //    failedMessage = $"UcbvHamtaObjekt got a hit but the lägenhetsnummer {lghNr} exists on multiple apartments in the object";
            //    return false;
            //}

            //var actualLgh = lgh.Single();

            //var boarea = actualLgh.Boarea;

            //if (!boarea.HasValue)
            //{
            //    failedMessage = $"UcbvHamtaObjekt got a hit on the apartment but no boarea is specified";
            //    return false;
            //}

            //var varderaResult = creditReportClient.UcbvVarderaBostadsratt(new UcbvVarderaBostadsrattRequest
            //{
            //    objektID = objectId,
            //    yta = boarea.Value.ToString(CultureInfo.InvariantCulture)
            //});

            //if (!varderaResult.Item1)
            //{
            //    failedMessage = $"UcbvVarderaBostadsratt failed: {varderaResult.Item3}";
            //    return false;
            //}

            //var actualVarderaResult = varderaResult.Item2;
            //ucbvResult = new Dictionary<string, string>();
            //ucbvResult["ucbvObjektId"] = objectId;
            //ucbvResult["brfNamn"] = objHit.Forening;
            //ucbvResult["brfLghSkvLghNr"] = actualLgh.Lghnr;
            //ucbvResult["brfLghYta"] = actualLgh.Boarea?.ToString(CultureInfo.InvariantCulture);
            //ucbvResult["brfLghVaning"] = actualLgh.Vaning?.ToString(CultureInfo.InvariantCulture);
            //ucbvResult["brfLghAntalRum"] = actualLgh.Rum;
            //ucbvResult["brfLghVarde"] = actualVarderaResult.Varde?.ToString(CultureInfo.InvariantCulture);
            //ucbvResult["brfSignalAr"] = actualVarderaResult?.Brfsignal?.Ar?.ToString(CultureInfo.InvariantCulture);
            //ucbvResult["brfSignalBelaning"] = actualVarderaResult?.Brfsignal?.GetBelaningCode();
            //ucbvResult["brfSignalLikviditet"] = actualVarderaResult?.Brfsignal?.GetLikviditetCode();
            //ucbvResult["brfSignalSjalvforsorjningsgrad"] = actualVarderaResult?.Brfsignal?.GetSjalvforsorjningsgradCode();
            //ucbvResult["brfSignalRantekanslighet"] = actualVarderaResult?.Brfsignal?.GetRantekanslighetCode();
            //ucbvResult["brfLghDebtAmount"] = actualVarderaResult?.Andelskulder?.ToString();

            //failedMessage = null;

            //return true;
        }
    }

    public class TestUcbvService : IUcbvService
    {
        private readonly UcbvService normalUcbvService;
        private readonly IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository;
        private readonly IMortgageLoanObjectService mortgageLoanObjectService;

        public TestUcbvService(UcbvService normalUcbvService, IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository, IMortgageLoanObjectService mortgageLoanObjectService)
        {
            this.normalUcbvService = normalUcbvService;
            this.partialCreditApplicationModelRepository = partialCreditApplicationModelRepository;
            this.mortgageLoanObjectService = mortgageLoanObjectService;
        }

        public bool TryAcceptValuation(string applicationNr, Dictionary<string, string> ucbvResult, out string failedMessage, out string valuationId)
        {
            return this.normalUcbvService.TryAcceptValuation(applicationNr, ucbvResult, out failedMessage, out valuationId);
        }

        public bool TryAutomateValution(string applicationNr, out string failedMessage, out Dictionary<string, string> ucbvResult)
        {
            //TODO: Requires a cross client model for objects
            throw new NotImplementedException();
            //var objectInfo = this.mortgageLoanObjectService.GetObject(applicationNr);
            //var propertyDetails = objectInfo.CondominiumPropertyDetails;

            //ucbvResult = new Dictionary<string, string>();
            //ucbvResult["ucbvObjektId"] = Guid.NewGuid().ToString();
            //ucbvResult["brfNamn"] = propertyDetails?.AssociationName + " (test)";
            //ucbvResult["brfLghSkvLghNr"] = propertyDetails ?.ApartmentNumber?.ToString();
            //ucbvResult["brfLghYta"] = propertyDetails?.LivingArea?.ToString();
            //ucbvResult["brfLghVaning"] = propertyDetails?.Floor?.ToString();
            //ucbvResult["brfLghAntalRum"] = propertyDetails?.NumberOfRooms?.ToString();
            //ucbvResult["brfLghDebtAmount"] = ((int)Math.Round(((decimal)objectInfo.PropertyEstimatedValue.Value) / 10m)).ToString();

            //if (objectInfo.PropertyEstimatedValue.HasValue)
            //    ucbvResult["brfLghVarde"] = (objectInfo.PropertyEstimatedValue.Value - 7500).ToString(CultureInfo.InvariantCulture);
            //else
            //    ucbvResult["brfLghVarde"] = "";

            //ucbvResult["brfSignalAr"] = "2018";
            //ucbvResult["brfSignalBelaning"] = "Ok";
            //ucbvResult["brfSignalLikviditet"] = "Ok";
            //ucbvResult["brfSignalSjalvforsorjningsgrad"] = "Ok";
            //ucbvResult["brfSignalRantekanslighet"] = "Ok";

            //failedMessage = null;

            //return true;
        }
    }

    public interface IUcbvService
    {
        bool TryAutomateValution(string applicationNr, out string failedMessage, out Dictionary<string, string> ucbvResult);

        bool TryAcceptValuation(string applicationNr, Dictionary<string, string> ucbvResult, out string failedMessage, out string valuationId);
    }
}