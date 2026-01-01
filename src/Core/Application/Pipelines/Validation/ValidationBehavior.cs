using FluentValidation;
using MediatR;

namespace Mootable.Application.Pipelines.Validation;

/// <summary>
/// FluentValidation ile MediatR entegrasyonu.
/// 
/// NEDEN AYRI VALIDATOR DOSYALARI:
/// Handler içinde validation yapmak, handler'ı şişirir ve test zorlaştırır.
/// Ayrı validator = ayrı unit test = daha güvenilir validation.
/// 
/// PRODUCTION DENEYİMİ:
/// Validation'ı handler içinde yapan projelerde şu pattern görülür:
/// - İlk 6 ay: "Çok basit, ayrı dosyaya gerek yok"
/// - 12 ay: Handler 500 satır, validation 200 satır
/// - 18 ay: "Validation logic'i değiştirmek için handler'ı refactor etmem lazım"
/// - 24 ay: Technical debt backlog'da "validation refactoring" item'ı
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}
