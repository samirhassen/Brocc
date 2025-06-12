using System.Collections.Generic;
using nSavings.ViewModel.FixedRateProduct.Common;

namespace nSavings.ViewModel.FixedRateProduct;

public class FixedRateProductManagementViewModel
{
    public List<ProductViewModel> FutureProducts { get; set; }
    public List<ProductViewModel> ActiveProducts { get; set; }
    public List<ProductViewModel> HistoricalProducts { get; set; }
    public List<AuditLogItem> AuditLog { get; set; }
}