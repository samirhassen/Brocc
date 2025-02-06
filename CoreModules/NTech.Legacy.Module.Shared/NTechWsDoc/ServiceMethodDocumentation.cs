using System.Collections.Generic;

namespace NTech.Services.Infrastructure.NTechWsDoc
{
    public class ServiceMethodDocumentation
    {
        public string Path { get; set; }
        public string Method { get; set; }
        public CompoundType RequestType { get; set; }
        public CompoundType ResponseType { get; set; }
        public string RequestExample { get; set; }
        public string ResponseExample { get; set; }
        public List<CompoundType> OtherTypes { get; set; }
    }
}
