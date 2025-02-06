using NTech.Core.PreCredit.Shared;
using System;

namespace nPreCredit
{
    public interface IPartialCreditApplicationModelRepositoryExtended : IPartialCreditApplicationModelRepository
    {
        PartialCreditApplicationModelExtended<TCustom> GetExtended<TCustom>(string applicationNr, PartialCreditApplicationModelRequest request, Func<string, IPreCreditContextExtended, TCustom> loadCustomDataByApplicationNr) where TCustom : PartialCreditApplicationModelExtendedCustomDataBase;
    }
}