using FluentValidation;
using Mootable.Application.Features.Auth.Constants;

namespace Mootable.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(AuthMessages.Validation.EmailRequired)
            .EmailAddress().WithMessage(AuthMessages.Validation.EmailInvalid);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(AuthMessages.Validation.PasswordRequired);
    }
}
