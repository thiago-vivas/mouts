using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Services;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Domain.Validation;

public class SaleItemValidator : AbstractValidator<SaleItem>
{
    public SaleItemValidator()
    {
        RuleFor(i => i.Product).NotNull();
        RuleFor(i => i.Product.Id).NotEmpty().When(i => i.Product != null);
        RuleFor(i => i.Product.Name).NotEmpty().When(i => i.Product != null);
        RuleFor(i => i.Quantity)
            .GreaterThan(0)
            .LessThanOrEqualTo(DiscountPolicy.MaxQuantityPerItem)
            .WithMessage($"Quantity must be between 1 and {DiscountPolicy.MaxQuantityPerItem}.");
        RuleFor(i => i.UnitPrice).GreaterThan(0);
    }
}
