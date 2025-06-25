using System;
using System.Linq;
using NTech.Services.Infrastructure;

namespace nSavings.Code.Riksgalden;

public class RiksgaldenConfiguration
{
    public string DepositorGuaranteeInstituteName { get; set; }
    public string DepositorGuaranteeOrgnr { get; set; }

    public static RiksgaldenConfiguration Create(IClientConfiguration clientConfig)
    {
        var s = clientConfig.GetCustomSection("SavingsRiksgalden");
        var instituteName = s?.Descendants()
            .SingleOrDefault(x => x.Name.LocalName == "DepositorGuaranteeInstituteName")?.Value;
        var instituteOrgnr = s?.Descendants()?.SingleOrDefault(x => x.Name.LocalName == "DepositorGuaranteeOrgnr")
            ?.Value;

        if (string.IsNullOrWhiteSpace(instituteName))
            throw new Exception(
                "Missing required client config value SavingsRiksgalden.DepositorGuaranteeInstituteName");

        if (string.IsNullOrWhiteSpace(instituteOrgnr))
            throw new Exception("Missing required client config value SavingsRiksgalden.DepositorGuaranteeOrgnr");

        return new RiksgaldenConfiguration
        {
            DepositorGuaranteeInstituteName = instituteName,
            DepositorGuaranteeOrgnr = instituteOrgnr
        };
    }
}