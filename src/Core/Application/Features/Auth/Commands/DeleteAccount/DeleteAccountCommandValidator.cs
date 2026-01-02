using FluentValidation;

namespace Mootable.Application.Features.Auth.Commands.DeleteAccount;

/// <summary>
/// Validator for delete account command
/// </summary>
public sealed class DeleteAccountCommandValidator : AbstractValidator<DeleteAccountCommand>
{
    public DeleteAccountCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required for verification");

        RuleFor(x => x.ConfirmationText)
            .NotEmpty().WithMessage("Confirmation text is required")
            .Equal("DELETE MY ACCOUNT").WithMessage("Please type 'DELETE MY ACCOUNT' to confirm");
    }
}