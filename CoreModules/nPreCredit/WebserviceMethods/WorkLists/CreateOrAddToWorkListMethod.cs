using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods
{
    public class CreateOrAddToWorkListMethod : TypedWebserviceMethod<CreateOrAddToWorkListMethod.Request, CreateOrAddToWorkListMethod.Response>
    {
        public override string Path => "WorkLists/CreateOrAddTo";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var s = requestContext.Resolver().Resolve<IWorkListService>();

            var items = request.Items.Select(x =>
                (
                    ItemId: x.ItemId,
                    Properties: x.Properties.Select(y => y.ToInternalProperty(s)).ToList())
                ).ToList();

            var result = s.CreateOrAddToWorkList(request.ExistingWorkListId, request.LeaveListUnderConstruction.GetValueOrDefault(),
                request.NewListType, request.NewListFilters?.Select(x => (Name: x.Name, Value: x.Value))?.ToList(), items);

            return new Response
            {
                WorkListId = result.WorkListId,
                WasAdded = result.WasAdded,
                WasCreated = result.WasCreated,
                WasLeftUnderConstruction = result.WasLeftUnderConstruction
            };
        }

        public class Request
        {
            public int? ExistingWorkListId { get; set; }
            public bool? LeaveListUnderConstruction { get; set; }
            public string NewListType { get; set; }
            public List<Filter> NewListFilters { get; set; }

            [Required]
            public List<Item> Items { get; set; }

            public class Item
            {
                [Required]
                public string ItemId { get; set; }
                [Required]
                public List<ItemProperty> Properties { get; set; }
            }

            public class Filter
            {
                public string Name { get; set; }
                public string Value { get; set; }
            }

            public class ItemProperty
            {
                [Required]
                public string Name { get; set; }
                public string StringValue { get; set; }
                public decimal? NumericValue { get; set; }
                public DateTime? DateValue { get; set; }

                public (string Name, string Value, string DataTypeName) ToInternalProperty(IWorkListService s)
                {
                    if (NumericValue.HasValue)
                        return s.CreateProperty(Name, NumericValue.Value);
                    else if (DateValue.HasValue)
                        return s.CreateProperty(Name, DateValue.Value);
                    else if (!string.IsNullOrWhiteSpace(StringValue))
                        return s.CreateProperty(Name, StringValue);
                    else
                        throw new NTechWebserviceMethodException(
                            "One of NumericValue, DateValue and StringValue should be set")
                        {
                            ErrorCode = "invalidItem",
                            IsUserFacing = true,
                            ErrorHttpStatusCode = 400
                        };
                }
            }
        }

        public class Response
        {
            public int? WorkListId { get; set; }
            public bool WasAdded { get; set; }
            public bool WasLeftUnderConstruction { get; set; }

            public bool WasCreated { get; set; }
        }
    }
}