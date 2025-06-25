using System;

namespace nSavings.ViewModel.FixedRateProduct.Common;

public enum ResponseStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
}

public class ProductViewModel
{
    public Guid? Id { get; set; }
    public string Name { get; set; }
    public decimal InterestRate { get; set; }
    public int TermInMonths { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; }
    public string ApprovedBy { get; set; }
}