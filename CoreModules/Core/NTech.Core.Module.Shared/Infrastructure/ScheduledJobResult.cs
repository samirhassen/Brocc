using System.Collections.Generic;

namespace NTech.Core.Module.Shared.Infrastructure
{
    public class ScheduledJobResult
    {
        public List<string> Warnings { get; set; }
        public List<string> Errors { get; set; }
    }
}
