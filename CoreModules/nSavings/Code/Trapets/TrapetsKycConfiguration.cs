using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace nSavings.Code.Trapets
{
    public class TrapetsKycConfiguration
    {
        public string IdSuffix { get; set; }
        public string BaseAccountType { get; set; }
        public string BaseAccountRisk { get; set; }
        public string BaseCustomerType { get; set; }
        public List<string> ApplicationKycQuestionNamesToTransfer { get; set; }
        public string ExportFileNamePattern { get; set; }

        public static TrapetsKycConfiguration FromXElement(XElement d)
        {
            var r = new TrapetsKycConfiguration
            {
                IdSuffix = d.Descendants().SingleOrDefault(x => x.Name == "IdSuffix")?.Value,
                BaseAccountType = d.Descendants().SingleOrDefault(x => x.Name == "BaseAccountType")?.Value,
                BaseAccountRisk = d.Descendants().SingleOrDefault(x => x.Name == "BaseAccountRisk")?.Value,
                BaseCustomerType = d.Descendants().SingleOrDefault(x => x.Name == "BaseCustomerType")?.Value,
                ExportFileNamePattern = d.Descendants().SingleOrDefault(x => x.Name == "ExportFileNamePattern")?.Value,
                ApplicationKycQuestionNamesToTransfer = d.Descendants()
                    .Where(x => x.Name == "ApplicationKycQuestionToTransfer").Select(x => x.Attribute("name")?.Value)
                    .Where(x => x != null).ToList()
            };
            return r;
        }
    }
}