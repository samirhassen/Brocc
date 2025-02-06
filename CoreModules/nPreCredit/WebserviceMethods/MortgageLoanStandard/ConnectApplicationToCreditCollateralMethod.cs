using Newtonsoft.Json;
using nPreCredit.Code;
using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.MortgageLoanStandard
{
    public class ConnectApplicationToCreditCollateralMethod : TypedWebserviceMethod<ConnectApplicationToCreditCollateralMethod.Request, ConnectApplicationToCreditCollateralMethod.Response>
    {
        public override string Path => "MortgageLoanStandard/Application/Connect-Credit-Collateral";

        public override bool IsEnabled => NEnv.IsStandardMortgageLoansEnabled;
        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("Middle");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);
            if (!(request.IsDisconnect == true || request.CreditCollateralId.HasValue))
            {
                return Error("IsDisconnect = true or a CreditCollateralId to connect is required");
            }

            var ai = requestContext
                .Resolver()
                .Resolve<ApplicationInfoService>()
                .GetApplicationInfo(request.ApplicationNr);

            if (!ai.IsActive)
                return Error("Application is not active");

            if (ai.IsFinalDecisionMade)
                return Error("Application cannot be changed");

            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                var applicationItems = context.ComplexApplicationListItems.Where(x =>
                        x.ApplicationNr == request.ApplicationNr
                        && x.ListName == "Application"
                        && x.Nr == 1)
                    .ToList();

                var currentCreditCollateralItem = applicationItems.SingleOrDefault(x => x.ItemName == "creditCollateralId");

                int? wasConnectedToCollateralId = null;
                int? wasDisconnectedFromCollateralId = null;

                List<ComplexApplicationListOperation> changes = null;
                if (request.IsDisconnect == true && currentCreditCollateralItem != null)
                {
                    changes = ComplexApplicationListService.CreateDeleteOperations(new List<ComplexApplicationListItem> { currentCreditCollateralItem });
                    ComplexApplicationListService.ChangeListComposable(changes, context);
                    context.SaveChanges();

                    wasDisconnectedFromCollateralId = int.Parse(currentCreditCollateralItem.ItemValue);
                }
                else if (request.CreditCollateralId.Value.ToString() != currentCreditCollateralItem?.ItemValue?.ToString())
                {
                    var creditCollateralId = request.CreditCollateralId.Value;
                    var creditClient = new CreditClient();
                    var collateral = creditClient.FetchCreditCollateral(creditCollateralId);
                    if (collateral == null)
                        return Error("No such collateral exists");
                    var itemsToSet = new Dictionary<string, string>
                    {
                        { "creditCollateralId", creditCollateralId.ToString() }
                    };
                    foreach (var nameToSynch in CollateralNamesToSynch)
                    {
                        if (collateral.CollateralItems.ContainsKey(nameToSynch))
                            itemsToSet[nameToSynch] = collateral.CollateralItems[nameToSynch].StringValue;
                    }

                    ComplexApplicationListService.SetUniqueItems(request.ApplicationNr, "Application", 1, itemsToSet, context);
                    var itemNamesToDelete = CollateralNamesToSynch.Except(itemsToSet.Keys).ToList();
                    if (itemNamesToDelete.Any())
                    {
                        var itemsToDelete = applicationItems.Where(x => itemNamesToDelete.Contains(x.ItemName)).ToList();
                        ComplexApplicationListService.ChangeListComposable(
                            ComplexApplicationListService.CreateDeleteOperations(itemsToDelete), context);
                    }

                    const string OwnerListName = "mortgageLoanPropertyOwner";
                    var currentOwnerCustomerIds = context
                        .CreditApplicationCustomerListMembers
                        .Where(x => x.ApplicationNr == request.ApplicationNr && x.ListName == OwnerListName)
                        .Select(x => x.CustomerId)
                        .ToHashSet();

                    var newOwnerCustomerIds = (request.OwnerCustomerIds ?? new List<int>()).ToHashSet();

                    var listService = requestContext.Resolver().Resolve<CreditApplicationCustomerListService>();
                    foreach (var customerIdToAdd in newOwnerCustomerIds.Except(currentOwnerCustomerIds))
                    {
                        listService.SetMemberStatusComposable(context, OwnerListName, true, customerIdToAdd, applicationNr: request.ApplicationNr);
                    }

                    foreach (var customerIdToRemove in currentOwnerCustomerIds.Except(newOwnerCustomerIds))
                    {
                        listService.SetMemberStatusComposable(context, OwnerListName, false, customerIdToRemove, applicationNr: request.ApplicationNr);
                    }

                    context.SaveChanges();

                    wasConnectedToCollateralId = creditCollateralId;
                }

                return new Response
                {
                    WasConnectedToCollateralId = wasConnectedToCollateralId,
                    WasDisconnectedFromCollateralId = wasDisconnectedFromCollateralId
                };
            }
        }

        private static HashSet<string> CollateralNamesToSynch = new HashSet<string>
        {
            "objectTypeCode",
            "seBrfOrgNr",
            "seBrfName",
            "seBrfApartmentNr",
            "seTaxOfficeApartmentNr",
            "objectId",
            "objectAddressStreet",
            "objectAddressZipcode",
            "objectAddressCity",
            "objectAddressMunicipality"
        };

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }

            public bool? IsDisconnect { get; set; }

            public int? CreditCollateralId { get; set; }

            public List<int> OwnerCustomerIds { get; set; }
        }

        public class Response
        {
            public int? WasConnectedToCollateralId { get; set; }
            public int? WasDisconnectedFromCollateralId { get; set; }
        }
    }
}