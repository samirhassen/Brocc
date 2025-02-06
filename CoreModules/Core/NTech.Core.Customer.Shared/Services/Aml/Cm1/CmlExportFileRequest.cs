using NTech.Core.Module.Shared.Clients;
using System;
using System.Collections.Generic;

namespace nCustomer.Code.Services.Aml.Cm1
{
    public class CompleteCmlExportFileRequest
    {
        public PerProductCmlExportFileRequest ProductRequest { get; set; }
        public List<CustomerModel> Customers { get; set; }

        public ExportTypeCode? ExportType { get; set; }
        public enum ExportTypeCode
        {
            Error = 0,
            Savings,
            Credit
        }

        public class CustomerModel
        {
            public int CustomerId { get; set; }
            public string TransferedStatus { get; set; }
        }
    }
}