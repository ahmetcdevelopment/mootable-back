using FluentValidation;
using Mootable.Application.Features.RabbitHoles.Constants;

namespace Mootable.Application.Features.RabbitHoles.Commands.CreateRabbitHole;

public sealed class CreateRabbitHoleCommandValidator : AbstractValidator<CreateRabbitHoleCommand>
{
    public CreateRabbitHoleCommandValidator()
    {
        RuleFor(x => x.MootTableId)
            .NotEmpty();

        RuleFor(x => x.StarterMessageId)
            .NotEmpty().WithMessage(RabbitHoleMessages.Validation.MessageIdRequired);

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage(RabbitHoleMessages.Validation.TitleRequired)
            .MinimumLength(3).WithMessage(RabbitHoleMessages.Validation.TitleMinLength)
            .MaximumLength(200).WithMessage(RabbitHoleMessages.Validation.TitleMaxLength);
    }
}
