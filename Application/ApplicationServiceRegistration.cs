using System.Reflection;
using Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register MediatR with all handlers from this assembly
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // Register AutoMapper profiles
        services.AddAutoMapper(cfg => { }, Assembly.GetExecutingAssembly());


        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceRegistration).Assembly, includeInternalTypes: true);

        // Register MediatR pipeline behaviors (order matters!)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }
}