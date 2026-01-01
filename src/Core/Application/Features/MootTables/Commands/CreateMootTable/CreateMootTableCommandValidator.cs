using FluentValidation;
using Mootable.Application.Features.MootTables.Constants;

namespace Mootable.Application.Features.MootTables.Commands.CreateMootTable;

public sealed class CreateMootTableCommandValidator : AbstractValidator<CreateMootTableCommand>
{
    public CreateMootTableCommandValidator()
    {
        RuleFor(x => x.ServerId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(MootTableMessages.Validation.NameRequired)
            .MinimumLength(2).WithMessage(MootTableMessages.Validation.NameMinLength)
            .MaximumLength(100).WithMessage(MootTableMessages.Validation.NameMaxLength);

        RuleFor(x => x.Topic)
            .MaximumLength(1024).WithMessage(MootTableMessages.Validation.TopicMaxLength)
            .When(x => x.Topic != null);
    }
}
