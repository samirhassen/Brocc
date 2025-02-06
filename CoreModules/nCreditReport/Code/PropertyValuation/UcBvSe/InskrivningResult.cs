using System.Collections.Generic;

namespace nCreditReport.Code.PropertyValuation.UcBvSe
{
    public class InskrivningResult
    {
        public List<AgandeModel> Agande { get; set; }

        public class AgandeModel
        {
            public string aktnummer { get; set; }
        }
    }
}