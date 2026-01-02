using FluentValidation;

namespace Mootable.Application.Features.Auth.Commands.ExternalLogin;

/// <summary>
/// Validator for external login command
/// </summary>
public sealed class ExternalLoginCommandValidator : AbstractValidator<ExternalLoginCommand>
{
    public ExternalLoginCommandValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("Provider is required")
            .Must(BeValidProvider).WithMessage("Invalid provider");

        RuleFor(x => x.ProviderKey)
            .NotEmpty().WithMessage("Provider key is required")
            .MaximumLength(256).WithMessage("Provider key is too long");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(256).WithMessage("Email is too long");

        RuleFor(x => x.DisplayName)
            .MaximumLength(100).WithMessage("Display name is too long")
            .When(x => !string.IsNullOrEmpty(x.DisplayName));

        RuleFor(x => x.PhotoUrl)
            .Must(BeValidUrl).WithMessage("Invalid photo URL")
            .When(x => !string.IsNullOrEmpty(x.PhotoUrl));

        RuleFor(x => x.IpAddress)
            .NotEmpty().WithMessage("IP address is required")
            .MaximumLength(45).WithMessage("IP address is too long");
    }

    private bool BeValidProvider(string provider)
    {
        var validProviders = new[] { "Google", "Microsoft", "GitHub", "Discord" };
        return validProviders.Contains(provider);
    }

    private bool BeValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) &&
               (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}