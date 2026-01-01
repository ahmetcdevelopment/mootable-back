using FluentValidation;
using Mootable.Application.Features.Servers.Constants;

namespace Mootable.Application.Features.Servers.Commands.CreateServer;

public sealed class CreateServerCommandValidator : AbstractValidator<CreateServerCommand>
{
    public CreateServerCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(ServerMessages.Validation.NameRequired)
            .MinimumLength(2).WithMessage(ServerMessages.Validation.NameMinLength)
            .MaximumLength(100).WithMessage(ServerMessages.Validation.NameMaxLength);

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage(ServerMessages.Validation.DescriptionMaxLength)
            .When(x => x.Description != null);
    }
}
