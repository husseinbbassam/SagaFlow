using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SagaFlow.Infrastructure.HealthChecks;

public static class HealthCheckConfiguration
{
    public static IServiceCollection AddInfrastructureHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var healthChecksBuilder = services.AddHealthChecks();

        var postgresConnectionString = configuration.GetConnectionString("SagaDb");
        if (!string.IsNullOrEmpty(postgresConnectionString))
        {
            healthChecksBuilder.AddNpgSql(
                postgresConnectionString,
                name: "postgresql",
                tags: new[] { "database", "postgres" });
        }

        var rabbitMqHost = configuration["RabbitMq:Host"] ?? "localhost";
        var rabbitMqUsername = configuration["RabbitMq:Username"] ?? "guest";
        var rabbitMqPassword = configuration["RabbitMq:Password"] ?? "guest";

        healthChecksBuilder.AddRabbitMQ(
            sp => 
            {
                var factory = new RabbitMQ.Client.ConnectionFactory
                {
                    HostName = rabbitMqHost,
                    UserName = rabbitMqUsername,
                    Password = rabbitMqPassword
                };
                return factory.CreateConnectionAsync().GetAwaiter().GetResult();
            },
            name: "rabbitmq",
            tags: new[] { "messaging", "rabbitmq" });

        return services;
    }
}
