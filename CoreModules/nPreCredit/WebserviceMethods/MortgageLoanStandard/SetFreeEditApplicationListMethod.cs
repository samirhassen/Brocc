using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.MortgageLoanStandard
{
    /// <summary>
    /// Allows free direct edit from the ui of a restricted subset of the ListNames of ComplexApplicationList.
    /// </summary>
    public class SetFreeEditApplicationListMethod : TypedWebserviceMethod<SetFreeEditApplicationListMethod.Request, SetFreeEditApplicationListMethod.Response>
    {
        public override string Path => "MortgageLoanStandard/Set-FreeEdit-Application-List";

        public override bool IsEnabled => NEnv.IsStandardMortgageLoansEnabled;

        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("Middle");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            if (request.Changes == null || request.Changes.Count == 0)
                return new Response();

            var resolver = requestContext.Resolver();
            var listService = resolver.Resolve<IComplexApplicationListService>();

            var editedListNames = request.Changes.Select(x => x.ListName).ToHashSet();
            if (editedListNames.Except(AllowedListNames).Any())
                return Error($"Only these list can be edited: {string.Join(", ", AllowedListNames)}");

            var complexListChanges = new List<ComplexApplicationListOperation>();

            foreach (var item in request.Changes)
            {
                complexListChanges.Add(new ComplexApplicationListOperation
                {
                    ApplicationNr = request.ApplicationNr,
                    ListName = item.ListName,
                    ItemName = item.ItemName,
                    Nr = item.Nr.Value,
                    IsDelete = string.IsNullOrWhiteSpace(item.ItemValue),
                    UniqueValue = string.IsNullOrWhiteSpace(item.ItemValue) ? null : item.ItemValue.Trim()
                });
            }

            if (request.RemoveOtherItemsInLists)
            {
                using (var context = new PreCreditContext())
                {
                    var existingItems = context
                        .ComplexApplicationListItems
                        .Where(x => x.ApplicationNr == request.ApplicationNr && editedListNames.Contains(x.ListName))
                        .ToList();
                    foreach (var existingItem in existingItems)
                    {
                        if (!complexListChanges.Any(x => x.ListName == existingItem.ListName
                            && x.Nr == existingItem.Nr
                            && x.ItemName == existingItem.ItemName
                            && !existingItem.IsRepeatable))
                        {
                            complexListChanges.Add(new ComplexApplicationListOperation
                            {
                                ApplicationNr = existingItem.ApplicationNr,
                                ListName = existingItem.ListName,
                                Nr = existingItem.Nr,
                                ItemName = existingItem.ItemName,
                                IsDelete = true
                            });
                        }
                    }
                }
            }

            listService.ChangeList(complexListChanges);

            return new Response();
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }

            public List<ListItem> Changes { get; set; }

            /// <summary>
            /// Set this to true to have other currently existing items in the lists be removed
            /// </summary>
            public bool RemoveOtherItemsInLists { get; set; }

            public class ListItem
            {
                [Required]
                public string ListName { get; set; }
                [Required]
                public int? Nr { get; set; }
                [Required]
                public string ItemName { get; set; }

                //Leave this empty/null to remove an existing item
                public string ItemValue { get; set; }
            }
        }

        public class Response { }

        /// <summary>
        /// Warning: Do NOT add things like Application, Applicant or other things used for workflow logic here since the
        /// user can then get around serverside check.
        /// 
        /// This should only be used for models that are user or handler edited data.
        /// </summary>
        public static HashSet<string> AllowedListNames = new HashSet<string>
        {
            "CreditorParty", "BrokerParty", "BrfAdminParty"
        };
    }

}