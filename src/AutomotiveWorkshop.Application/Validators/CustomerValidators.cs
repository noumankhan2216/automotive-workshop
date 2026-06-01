using AutomotiveWorkshop.Application.DTOs.Customers;
using FluentValidation;

namespace AutomotiveWorkshop.Application.Validators;

public class CreateCustomerValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(30);
    }
}

public class UpdateCustomerValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(30);
    }
}
