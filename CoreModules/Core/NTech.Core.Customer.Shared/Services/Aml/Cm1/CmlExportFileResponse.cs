using System.Collections.Generic;

namespace nCustomer.Code.Services.Aml.Cm1
{
    public class CmlExportFileResponse
    {
        public string CustomerFileArchiveKey { get; set; }
        public List<string> TransactionFileArchiveKeys { get; set; }
    }
}