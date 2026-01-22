using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SagaFlow.Infrastructure.Persistence;

namespace SagaFlow.Infrastructure.Messaging;

public static class MassTransitConfiguration
{
    public static IServiceCollection AddMassTransitWithRabbitMq(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureSagas = null)
    {
        services.AddMassTransit(x =>
        {
            configureSagas?.Invoke(x);

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMqHost = configuration["RabbitMq:Host"] ?? "localhost";
                var rabbitMqUsername = configuration["RabbitMq:Username"] ?? "guest";
                var rabbitMqPassword = configuration["RabbitMq:Password"] ?? "guest";

                cfg.Host(rabbitMqHost, "/", h =>
                {
                    h.Username(rabbitMqUsername);
                    h.Password(rabbitMqPassword);
                });

                cfg.UseMessageRetry(r => r.Exponential(
                    retryLimit: 5,
                    minInterval: TimeSpan.FromSeconds(2),
                    maxInterval: TimeSpan.FromSeconds(30),
                    intervalDelta: TimeSpan.FromSeconds(2)));

                cfg.UseDelayedRedelivery(r => r.Exponential(
                    retryLimit: 3,
                    minInterval: TimeSpan.FromSeconds(5),
                    maxInterval: TimeSpan.FromMinutes(1),
                    intervalDelta: TimeSpan.FromSeconds(5)));

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
