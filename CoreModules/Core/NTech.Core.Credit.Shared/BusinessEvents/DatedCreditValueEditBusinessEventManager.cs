using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using System.Linq;

namespace nCredit.DbModel.BusinessEvents
{
    public class DatedCreditValueEditBusinessEventManager : BusinessEventManagerOrServiceBase
    {
        private readonly CreditContextFactory contextFactory;

        public DatedCreditValueEditBusinessEventManager(INTechCurrentUserMetadata metadata, ICoreClock clock, IClientConfigurationCore clientConfiguration, CreditContextFactory contextFactory) : base(metadata, clock, clientConfiguration)
        {
            this.contextFactory = contextFactory;
        }

        public DatedCreditValue SetValueComposable(BusinessEvent evt, CreditHeader credit, ICreditContextExtended context, DatedCreditValueCode datedCreditValueCode, decimal value)
        {
            return AddDatedCreditValue(datedCreditValueCode.ToString(), value, credit, evt, context);
        }

        public DatedCreditValue SetValue(BusinessEventType businessEventType, string creditNr, DatedCreditValueCode datedCreditValueCode, decimal value)
        {
            using (var context = contextFactory.CreateContext())
            {
                var credit = context.CreditHeadersQueryable.SingleOrDefault(x => x.CreditNr == creditNr);
                if (credit == null)
                    throw new NTechCoreWebserviceException("No such credit exists") { ErrorCode = "noSuchCreditExists", ErrorHttpStatusCode = 400, IsUserFacing = true };
                if (credit.Status != CreditStatus.Normal.ToString())
                    throw new NTechCoreWebserviceException("Credit is not active") { ErrorCode = "creditIsNotActive", ErrorHttpStatusCode = 400, IsUserFacing = true };

                var evt = AddBusinessEvent(businessEventType, context);

                var newValue = SetValueComposable(evt, credit, context, datedCreditValueCode, value);

                context.SaveChanges();

                return newValue;
            }
        }
    }
}