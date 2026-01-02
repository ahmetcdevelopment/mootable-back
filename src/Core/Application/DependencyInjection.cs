using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Mootable.Application.Features.Auth.Rules;
using Mootable.Application.Features.MootTables.Rules;
using Mootable.Application.Features.Servers.Rules;
using Mootable.Application.Pipelines.Authorization;
using Mootable.Application.Pipelines.Caching;
using Mootable.Application.Pipelines.Logging;
using Mootable.Application.Pipelines.Transaction;
using Mootable.Application.Pipelines.Validation;

namespace Mootable.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CacheRemovingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

        services.AddScoped<AuthBusinessRules>();
        services.AddScoped<ServerBusinessRules>();
        services.AddScoped<MootTableBusinessRules>();

        return services;
    }
}
