using Anders.Warehouse.Web.Domain;
using FluentValidation;

namespace Anders.Warehouse.Web.Validation;

public class ProductValidator : AbstractValidator<Product>
{
    public ProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SupplierName).NotEmpty().MaximumLength(200);
    }
}

public class PurchaseLineValidator : AbstractValidator<PurchaseLine>
{
    public PurchaseLineValidator()
    {
        RuleFor(x => x.ProductName).NotEmpty();
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}
