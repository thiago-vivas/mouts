using Ambev.DeveloperEvaluation.Domain.Services;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

public class UpdateSaleValidator : AbstractValidator<UpdateSaleCommand>
{
    public UpdateSaleValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.SaleDate).NotEmpty();
        RuleFor(c => c.Customer.Id).NotEmpty();
        RuleFor(c => c.Customer.Name).NotEmpty();
        RuleFor(c => c.Branch.Id).NotEmpty();
        RuleFor(c => c.Branch.Name).NotEmpty();
        RuleFor(c => c.Items).NotEmpty().WithMessage("A sale must contain at least one item.");

        RuleForEach(c => c.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.ProductName).NotEmpty();
            item.RuleFor(i => i.UnitPrice).GreaterThan(0);
            item.RuleFor(i => i.Quantity)
                .GreaterThan(0)
                .LessThanOrEqualTo(DiscountPolicy.MaxQuantityPerItem)
                .WithMessage($"Cannot sell more than {DiscountPolicy.MaxQuantityPerItem} identical items.");
        });
    }
}
