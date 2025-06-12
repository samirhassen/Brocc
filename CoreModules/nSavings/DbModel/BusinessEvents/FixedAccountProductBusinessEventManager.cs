using System;
using System.Data.Entity;
using System.IdentityModel;
using System.Threading.Tasks;
using nSavings.DbModel.DbException;
using nSavings.ViewModel.FixedRateProduct.Common;
using NTech;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFixed;

namespace nSavings.DbModel.BusinessEvents;

public class FixedAccountProductBusinessEventManager(
    int userId,
    string informationMetadata,
    IClock clock,
    SavingsContext context = null)
    : BusinessEventManagerBase(userId, informationMetadata, clock), IDisposable
{
    private readonly SavingsContext _context = context ?? new SavingsContext();

    public async Task AddProduct(ProductViewModel model, string user)
    {
        if (!Validate(model, out var error))
        {
            throw new ValidationException(error);
        }

        _context.BeginTransaction();
        var evt = AddBusinessEvent(BusinessEventType.FixedRateProductAdded, _context);
        try
        {
            var dbm = MapProduct(model, evt, user);

            _context.FixedAccountProducts.Add(dbm);
            _context.FixedAccountProductAuditLog.Add(new FixedAccountProductAuditLog
            {
                Message = $"{user} added new product \"{model.Name}\"",
                User = user,
                CreatedAt = Clock.Now.DateTime,
                BusinessEvent = evt
            });

            await _context.SaveChangesAsync();

            _context.CommitTransaction();
        }
        catch (Exception)
        {
            _context.RollbackTransaction();
            throw;
        }
    }

    public async Task UpdateProduct(ProductViewModel newProduct, string user)
    {
        var currentProduct = await _context.FixedAccountProducts
            .SingleOrDefaultAsync(p => p.Id == newProduct.Id.ToString());

        if (currentProduct == null)
        {
            throw new EntityNotFoundException($"Product with id {newProduct.Id} was not found");
        }

        if (!ValidateUpdate(currentProduct, newProduct, out var error))
        {
            throw new ValidationException(error);
        }

        _context.BeginTransaction();
        try
        {
            var evt = AddBusinessEvent(BusinessEventType.FixedRateProductChanged, _context);
            ApplyUpdates(currentProduct, newProduct, user, evt);
            _context.FixedAccountProductAuditLog.Add(new FixedAccountProductAuditLog
            {
                Message = $"{user} updated product \"{currentProduct.Name}\"",
                User = user,
                CreatedAt = Clock.Now.DateTime,
                BusinessEvent = evt
            });

            await _context.SaveChangesAsync();

            _context.CommitTransaction();
        }
        catch (Exception)
        {
            _context.RollbackTransaction();
            throw;
        }
    }

    public async Task RespondToProductProposal(Guid guid, bool approved, string user)
    {
        _context.BeginTransaction();
        var id = guid.ToString();

        try
        {
            var product = await _context.FixedAccountProducts
                .SingleOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                throw new EntityNotFoundException($"Product with id {id} was not found");
            }

            if (product.UpdatedBy == user)
            {
                throw new BadRequestException("Product cannot be approved by the same user that proposed it");
            }

            var now = Clock.Now.DateTime;

            var evt = AddBusinessEvent(BusinessEventType.FixedRateProductResponse, _context);
            product.Response = approved;
            product.RespondedBy = user;
            product.RespondedAt = now;
            product.RespondedAtBusinessEvent = evt;
            _context.FixedAccountProductAuditLog.Add(new FixedAccountProductAuditLog
            {
                Message = approved
                    ? $"{user} approved product \"{product.Name}\""
                    : $"{user} rejected product \"{product.Name}\"",
                User = user,
                CreatedAt = now,
                BusinessEvent = evt
            });

            await _context.SaveChangesAsync();
            _context.CommitTransaction();
        }
        catch (Exception)
        {
            _context.RollbackTransaction();
            throw;
        }
    }

    public static bool RateValidAt(SavingsContext ctx, Guid productId, DateTime date)
    {
        var product = ctx.FixedAccountProducts.Find(productId.ToString());

        if (product == null) return false;
        return product.ValidFrom < date && (product.ValidTo == null || product.ValidTo > date);
    }

    private void ApplyUpdates(FixedAccountProduct currentProduct, ProductViewModel newProduct,
        string user, BusinessEvent evt)
    {
        FillInInfrastructureFields(currentProduct);
        currentProduct.Name = newProduct.Name;
        currentProduct.InterestRatePercent = newProduct.InterestRate;
        currentProduct.TermInMonths = newProduct.TermInMonths;
        currentProduct.ValidFrom = newProduct.ValidFrom;
        currentProduct.ValidTo = newProduct.ValidTo;
        currentProduct.UpdatedAtBusinessEvent = evt;
        currentProduct.UpdatedBy = user;
    }

    private bool Validate(ProductViewModel newProduct, out string error)
    {
        if (newProduct.InterestRate < 0)
        {
            error = "Interest rate must be positive";
            return false;
        }

        if (newProduct.TermInMonths < 0)
        {
            error = "Term in months must be positive";
            return false;
        }

        if (newProduct.ValidFrom < Clock.Now.DateTime)
        {
            error = "Product start date must be today or in the future";
            return false;
        }

        if (newProduct.ValidFrom >= newProduct.ValidTo)
        {
            error = "Product end date must come after start date";
            return false;
        }

        error = null;
        return true;
    }

    private bool ValidateUpdate(FixedAccountProduct currentProduct, ProductViewModel newProduct,
        out string error)
    {
        if (!Validate(newProduct, out error)) return false;
        error = "Only the end date can be changed on active or past products";
        if (currentProduct.Response != true || currentProduct.ValidFrom > DateTime.Now)
            return true; // Product not active, editing allowed
        // Active/Past product, only end date edits are allowed 
        return currentProduct.Name == newProduct.Name &&
               currentProduct.InterestRatePercent == newProduct.InterestRate &&
               currentProduct.TermInMonths == newProduct.TermInMonths &&
               currentProduct.ValidFrom == newProduct.ValidFrom;
    }

    private FixedAccountProduct MapProduct(ProductViewModel product, BusinessEvent evt, string user)
    {
        var res = new FixedAccountProduct
        {
            Id = Guid.NewGuid().ToString(),
            Name = product.Name,
            InterestRatePercent = product.InterestRate,
            TermInMonths = product.TermInMonths,
            ValidFrom = product.ValidFrom,
            ValidTo = product.ValidTo,
            CreatedAt = Clock.Now.DateTime,
            CreatedBy = user,
            CreatedAtBusinessEvent = evt,
            UpdatedBy = user,
            UpdatedAtBusinessEvent = evt
        };

        FillInInfrastructureFields(res);

        return res;
    }


    public void Dispose()
    {
        _context?.Dispose();
    }
}