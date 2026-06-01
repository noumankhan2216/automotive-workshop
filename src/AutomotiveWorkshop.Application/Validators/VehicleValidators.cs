using AutomotiveWorkshop.Application.DTOs.Vehicles;
using FluentValidation;

namespace AutomotiveWorkshop.Application.Validators;

public class CreateVehicleValidator : AbstractValidator<CreateVehicleRequest>
{
    public CreateVehicleValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Make).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Year).InclusiveBetween(1900, DateTime.UtcNow.Year + 1);
    }
}

public class UpdateVehicleValidator : AbstractValidator<UpdateVehicleRequest>
{
    public UpdateVehicleValidator()
    {
        RuleFor(x => x.Make).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Year).InclusiveBetween(1900, DateTime.UtcNow.Year + 1);
    }
}
