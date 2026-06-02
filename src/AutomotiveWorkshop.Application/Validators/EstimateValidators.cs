using AutomotiveWorkshop.Application.DTOs.Estimates;
using FluentValidation;

namespace AutomotiveWorkshop.Application.Validators;

public class CreateEstimateValidator : AbstractValidator<CreateEstimateRequest>
{
    public CreateEstimateValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.VehicleId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new EstimateItemValidator());
    }
}

public class UpdateEstimateValidator : AbstractValidator<UpdateEstimateRequest>
{
    public UpdateEstimateValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new EstimateItemValidator());
    }
}

public class EstimateItemValidator : AbstractValidator<EstimateItemRequest>
{
    public EstimateItemValidator()
    {
        RuleFor(i => i.Description).NotEmpty().MaximumLength(500);
        RuleFor(i => i.Quantity).GreaterThan(0);
        RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0);
    }
}
