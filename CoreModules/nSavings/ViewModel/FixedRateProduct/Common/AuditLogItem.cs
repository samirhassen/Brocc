using System;

namespace nSavings.ViewModel.FixedRateProduct.Common;

public class AuditLogItem
{
    public DateTime Date { get; set; }
    public string User { get; set; }
    public string Message { get; set; }
}