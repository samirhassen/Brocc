using NTech.Banking.PluginApis.CreateApplication;
using System.Collections.Generic;

namespace nPreCredit.Code.Services
{
    public interface ICampaignCodeService
    {
        List<CreateApplicationRequestModel.ComplexItem> MatchCampaignOnCreateApplication(List<CreateApplicationRequestModel.ComplexItem> currentItems);
    }
}