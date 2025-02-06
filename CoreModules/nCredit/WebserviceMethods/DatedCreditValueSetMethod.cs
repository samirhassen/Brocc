using nCredit.DbModel.BusinessEvents;
using NTech.Banking.Conversion;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nCredit.WebserviceMethods
{
    public class DatedCreditValueSetMethod : TypedWebserviceMethod<DatedCreditValueSetMethod.Request, DatedCreditValueSetMethod.Response>
    {
        public override string Path => "DatedCreditValue/Set";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var datedCreditValueCode = Enums.Parse<DatedCreditValueCode>(request.DatedCreditValueCode);
            if (!datedCreditValueCode.HasValue)
                return Error("No such DatedCreditValueCode exists", errorCode: "noSuchDatedCreditValueCodeExists");
            var businessEventType = Enums.Parse<BusinessEventType>(request.BusinessEventType);
            if (!businessEventType.HasValue)
                return Error("No such BusinessEventType exists", errorCode: "noSuchBusinessEventTypeExists");

            var allowedBusinessEventTypes = AllowedEventTypesPerCode.Opt(datedCreditValueCode.Value);
            if (allowedBusinessEventTypes == null)
                return Error("Editing this DatedCreditValueCode is not allowed", errorCode: "datedCreditValueCodeNotAllowed");

            if (!allowedBusinessEventTypes.Contains(businessEventType.Value))
                return Error(
                    "For this DatedCreditValueCode only these BusinessEventTypes are allowed: "
                    + string.Join(", ", allowedBusinessEventTypes.Select(x => x.ToString())),
                    errorCode: "businessEventTypeNotAllowed");

            var services = requestContext.Service();
            var mgr = new DatedCreditValueEditBusinessEventManager(requestContext.CurrentUserMetadata(), CoreClock.SharedInstance, NEnv.ClientCfgCore,
                services.ContextFactory);

            var storedValue = Math.Round(request.Value.Value, 2); //Round so we dont have to do a database roundtrip to ensure that Response.NewValue matches what is stored always.
            var newValue = mgr.SetValue(businessEventType.Value, request.CreditNr, datedCreditValueCode.Value, storedValue);

            return new Response
            {
                NewValue = newValue.Value,
                BusinessEventId = newValue.BusinessEventId
            };
        }

        /// <summary>
        /// Why is this here?
        /// 
        /// Having a completely open function could potentially allow security issues.
        /// There have been such in the past with KeyValueStore set allowing bypassing duality for instance.
        /// 
        /// On the other hand having a separate webservice api for every single value to edit seems to be alot of work
        /// for very little reward.
        /// 
        /// This is an attempt at a reasonable compromise where future things to edit can use this rather than writing a new one
        /// but will have to add the usecase to this whitelist.
        /// </summary>
        private static Dictionary<DatedCreditValueCode, ISet<BusinessEventType>> AllowedEventTypesPerCode = new Dictionary<DatedCreditValueCode, ISet<BusinessEventType>>
        {
            { DatedCreditValueCode.ApplicationLossGivenDefault, new HashSet<BusinessEventType> { BusinessEventType.SetApplicationLossGivenDefault } },
            { DatedCreditValueCode.ApplicationProbabilityOfDefault, new HashSet<BusinessEventType> { BusinessEventType.SetApplicationProbabilityOfDefault } }
        };

        public class Request
        {
            [Required]
            public string CreditNr { get; set; }

            [Required]
            public string DatedCreditValueCode { get; set; }

            [Required]
            public string BusinessEventType { get; set; }

            [Required]
            public decimal? Value { get; set; }
        }

        public class Response
        {
            public decimal? NewValue { get; set; } //NOTE: Nullable to support a possible future usecase of removing values
            public int BusinessEventId { get; set; }
        }
    }
}