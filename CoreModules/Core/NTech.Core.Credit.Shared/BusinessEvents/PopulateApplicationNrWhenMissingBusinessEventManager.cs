using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.DbModel.BusinessEvents
{
    /// <summary>
    /// One time corretion job to handle the fact that earlier versions of the system did not send applicationnr along to the credit module.
    /// </summary>
    public class PopulateApplicationNrWhenMissingBusinessEventManager : DbModel.BusinessEvents.BusinessEventManagerOrServiceBase
    {
        private readonly INTechServiceRegistry serviceRegistry;
        private readonly CreditContextFactory contextFactory;
        private readonly IPreCreditClient preCreditClient;

        public PopulateApplicationNrWhenMissingBusinessEventManager(INTechCurrentUserMetadata currentUser, ICoreClock clock, IClientConfigurationCore clientConfiguration,
            INTechServiceRegistry serviceRegistry, CreditContextFactory contextFactory, IPreCreditClient preCreditClient) : base(currentUser, clock, clientConfiguration)
        {
            this.serviceRegistry = serviceRegistry;
            this.contextFactory = contextFactory;
            this.preCreditClient = preCreditClient;
        }

        public int PopulateReturningCount()
        {
            int count = 0;
            if (!serviceRegistry.ContainsService("nPreCredit"))
                return count;

            using (var context = contextFactory.CreateContext())
            {
                var creditNrs = context
                    .CreditHeadersQueryable
                    .Where(x => !x.DatedCreditStrings.Any(y => y.Name == DatedCreditStringCode.ApplicationNr.ToString()))
                    .Select(x => x.CreditNr)
                    .ToArray();

                if (creditNrs.Length == 0)
                    return count;

                BusinessEvent evt = null;

                foreach (var creditNrGroup in creditNrs.SplitIntoGroupsOfN(200))
                {
                    var applicationNrsByCreditNr = preCreditClient.GetApplicationNrsByCreditNrs(new HashSet<string>(creditNrGroup));
                    foreach (var k in applicationNrsByCreditNr)
                    {
                        if (evt == null)
                        {
                            evt = AddBusinessEvent(BusinessEventType.Correction, context);
                        }
                        AddDatedCreditString(DatedCreditStringCode.ApplicationNr.ToString(), k.Value, k.Key, evt, context);
                        count += 1;
                    }
                }

                if (evt != null)
                {
                    context.SaveChanges();
                }

                return count;
            }
        }
    }
}