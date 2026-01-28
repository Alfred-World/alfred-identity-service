using FluentValidation;

namespace Alfred.Identity.Application.Auth.Commands.Register;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(v => v.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not valid.");

        RuleFor(v => v.FullName)
            .NotEmpty().WithMessage("Full Name is required.")
            .MaximumLength(200).WithMessage("Full Name must not exceed 200 characters.");

        RuleFor(v => v.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Must(p => p.Any(char.IsUpper)).WithMessage("Password must contain at least one uppercase letter.")
            .Must(p => p.Any(char.IsLower)).WithMessage("Password must contain at least one lowercase letter.")
            .Must(p => p.Any(char.IsDigit)).WithMessage("Password must contain at least one digit.")
            .Must(p => p.Any(ch => !char.IsLetterOrDigit(ch)))
            .WithMessage("Password must contain at least one special character.");
    }
}
