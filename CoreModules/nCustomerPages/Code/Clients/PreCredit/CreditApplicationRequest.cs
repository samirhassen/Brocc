using System.Collections.Generic;

namespace nCustomerPages.Code
{
    public class CreditApplicationRequest
    {
        public string UserLanguage { get; set; }
        public int NrOfApplicants { get; set; }
        public Item[] Items { get; set; }
        public class Item
        {
            public string Group { get; set; }
            public string Name { get; set; }
            public string Value { get; set; }
        }
        public string ProviderName { get; set; }
        public string ApplicationRequestJson { get; set; }
        public List<ExternalVariableItem> ExternalVariables { get; set; }
        public class ExternalVariableItem
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }
    }
}