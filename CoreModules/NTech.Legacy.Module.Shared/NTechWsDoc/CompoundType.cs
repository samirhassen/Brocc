using System.Collections.Generic;

namespace NTech.Services.Infrastructure.NTechWsDoc
{
    public class CompoundType
    {
        public string Name { get; set; }
        public List<PrimtiveProperty> PrimtiveProperties { get; set; }
        public List<CompoundProperty> CompoundProperties { get; set; }
    }
}
