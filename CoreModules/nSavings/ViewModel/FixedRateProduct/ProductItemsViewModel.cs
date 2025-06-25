using System.Collections.Generic;
using nSavings.ViewModel.FixedRateProduct.Common;

namespace nSavings.ViewModel.FixedRateProduct;

public class ProductItemsViewModel(List<ProductViewModel> products, bool grayedOut = false, bool allowEdit = true)
{
    public List<ProductViewModel> Products { get; } = products;
    public bool GrayedOut { get; } = grayedOut;
    public bool AllowEdit { get; } = allowEdit;
}