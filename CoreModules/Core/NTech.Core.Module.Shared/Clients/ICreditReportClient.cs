using System;
using System.Collections.Generic;

namespace NTech.Core.Module.Shared.Clients
{
    public interface ICreditReportClient
    {
        List<FindForCustomerCreditReportModel> FindCreditReportsByReason(string reasonType, string reasonData, bool findCompanyReports);
        GetCreditReportByIdResult GetCreditReportById(int creditReportId, IList<string> requestedCreditReportFields);
    }

    public class FindForCustomerCreditReportModel
    {
        public int Id { get; set; }
        public DateTimeOffset RequestDate { get; set; }
        public string CreditReportProviderName { get; set; }
        public int CustomerId { get; set; }
        public bool HasReason { get; set; }
        public bool HasTableValuesPreview { get; set; }
        public string HtmlPreviewArchiveKey { get; set; }
        public string PdfPreviewArchiveKey { get; set; }
        public string RawXmlArchiveKey { get; set; }
    }

    public class GetCreditReportByIdResult
    {
        public int CreditReportId { get; set; }
        public DateTimeOffset RequestDate { get; set; }
        public int CustomerId { get; set; }
        public string ProviderName { get; set; }
        public List<Item> Items { get; set; }
        public class Item
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }
    }
}
