using FluentValidation;
using Mootable.Application.Features.Auth.Constants;

namespace Mootable.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(AuthMessages.Validation.EmailRequired)
            .EmailAddress().WithMessage(AuthMessages.Validation.EmailInvalid);

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage(AuthMessages.Validation.UsernameRequired)
            .MinimumLength(3).WithMessage(AuthMessages.Validation.UsernameMinLength)
            .MaximumLength(32).WithMessage(AuthMessages.Validation.UsernameMaxLength)
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage(AuthMessages.Validation.UsernameInvalidChars);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(AuthMessages.Validation.PasswordRequired)
            .MinimumLength(8).WithMessage(AuthMessages.Validation.PasswordMinLength)
            .Matches(@"[A-Z]").WithMessage(AuthMessages.Validation.PasswordRequiresUppercase)
            .Matches(@"[a-z]").WithMessage(AuthMessages.Validation.PasswordRequiresLowercase)
            .Matches(@"[0-9]").WithMessage(AuthMessages.Validation.PasswordRequiresDigit)
            .Matches(@"[!@#$%^&*(),.?""':{}|<>]").WithMessage(AuthMessages.Validation.PasswordRequiresSpecialChar);
    }
}
