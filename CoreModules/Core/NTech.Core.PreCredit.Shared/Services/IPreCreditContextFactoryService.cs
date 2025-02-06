using NTech.Core.PreCredit.Shared;

namespace nPreCredit.Code.Services
{
    public interface IPreCreditContextFactoryService
    {
        IPreCreditContextExtended CreateExtended();
    }
}