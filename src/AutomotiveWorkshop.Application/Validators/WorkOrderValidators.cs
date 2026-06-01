using AutomotiveWorkshop.Application.DTOs.WorkOrders;
using FluentValidation;

namespace AutomotiveWorkshop.Application.Validators;

public class CreateWorkOrderValidator : AbstractValidator<CreateWorkOrderRequest>
{
    public CreateWorkOrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.VehicleId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Description).NotEmpty().MaximumLength(500);
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}
