using NTech.Core.Customer.Shared.Models;
using NTech.Core.Module.Shared;
using System.Collections.Generic;

namespace NTech.Core.Customer.Shared
{
    public interface ICustomerEnvSettings : ISharedEnvSettings
    {
        Dictionary<string, KycQuestionsTemplate> DefaultKycQuestionsSets { get; }
        string RelativeKycLogFolder { get; }
        string LogFolder { get; }

    }
}