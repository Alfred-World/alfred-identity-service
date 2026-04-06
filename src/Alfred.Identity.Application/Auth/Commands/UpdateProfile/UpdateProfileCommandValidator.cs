using FluentValidation;

namespace Alfred.Identity.Application.Auth.Commands.UpdateProfile;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.FullName.Value)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters")
            .When(x => x.FullName.HasValue);

        RuleFor(x => x.PhoneNumber.Value)
            .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters")
            .When(x => x.PhoneNumber.HasValue && x.PhoneNumber.Value != null);
    }
}
