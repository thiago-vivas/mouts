using Ambev.DeveloperEvaluation.Domain.Entities;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Domain.Validation;

public class SaleValidator : AbstractValidator<Sale>
{
    public SaleValidator()
    {
        RuleFor(s => s.SaleNumber).NotEmpty();
        RuleFor(s => s.SaleDate).NotEmpty();
        RuleFor(s => s.Customer).NotNull();
        RuleFor(s => s.Customer.Id).NotEmpty().When(s => s.Customer != null);
        RuleFor(s => s.Customer.Name).NotEmpty().When(s => s.Customer != null);
        RuleFor(s => s.Branch).NotNull();
        RuleFor(s => s.Branch.Id).NotEmpty().When(s => s.Branch != null);
        RuleFor(s => s.Branch.Name).NotEmpty().When(s => s.Branch != null);
        RuleFor(s => s.Items).NotEmpty().WithMessage("A sale must contain at least one item.");
        RuleForEach(s => s.Items).SetValidator(new SaleItemValidator());
    }
}
