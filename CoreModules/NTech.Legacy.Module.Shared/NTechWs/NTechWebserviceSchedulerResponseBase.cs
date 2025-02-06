using System.Collections.Generic;

namespace NTech.Services.Infrastructure.NTechWs
{
    public class NTechWebserviceSchedulerResponseBase
    {
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }
    }
}
