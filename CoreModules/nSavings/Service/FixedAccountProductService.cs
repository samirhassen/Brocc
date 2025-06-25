using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using nSavings.DbModel;
using nSavings.ViewModel.FixedRateProduct.Common;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFixed;

namespace nSavings.Service;

public class FixedAccountProductService(in SavingsContext context = null) : IDisposable
{
    private readonly SavingsContext _context = context ?? new SavingsContext();

    public async Task<ProductViewModel> GetProduct(Guid guid)
    {
        return MapProduct(await _context.FixedAccountProducts
            .SingleOrDefaultAsync(p => p.Id == guid.ToString()));
    }

    public async Task<List<ProductViewModel>> GetAllProducts()
    {
        var products = await _context.FixedAccountProducts
            .OrderBy(p => p.ValidFrom)
            .ToListAsync();

        return products.Select(MapProduct).ToList();
    }

    public async Task<List<ProductViewModel>> GetActiveProductsAt(DateTime date)
    {
        var products = await _context.FixedAccountProducts
            .Where(p => p.Response == true &&
                        p.ValidFrom <= date &&
                        (p.ValidTo == null || p.ValidTo > date))
            .OrderBy(p => p.ValidFrom)
            .ToListAsync();

        return products.Select(MapProduct).ToList();
    }

    public async Task<List<AuditLogItem>> GetAuditLog(int offset = 0, int count = 20)
    {
        var log = await _context.FixedAccountProductAuditLog
            .OrderBy(l => l.CreatedAt)
            .Skip(offset)
            .Take(count)
            .ToListAsync();

        return log.Select(MapAuditLog).ToList();
    }

    private static AuditLogItem MapAuditLog(FixedAccountProductAuditLog log)
    {
        return new AuditLogItem
        {
            Date = log.CreatedAt,
            User = log.User,
            Message = log.Message
        };
    }

    private static ProductViewModel MapProduct(FixedAccountProduct product)
    {
        var guid = Guid.Parse(product.Id);

        return new ProductViewModel
        {
            Id = guid,
            Name = product.Name,
            InterestRate = product.InterestRatePercent,
            TermInMonths = product.TermInMonths,
            ResponseStatus = product.Response switch
            {
                true => ResponseStatus.Approved,
                false => ResponseStatus.Rejected,
                null => ResponseStatus.Pending
            },
            ApprovedBy = product.RespondedBy,
            ValidFrom = product.ValidFrom,
            ValidTo = product.ValidTo,
            CreatedAt = product.CreatedAt,
            CreatedBy = product.CreatedBy,
            UpdatedAt = product.ChangedDate.DateTime,
            UpdatedBy = product.UpdatedBy
        };
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}