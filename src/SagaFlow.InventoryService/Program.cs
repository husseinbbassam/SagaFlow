using SagaFlow.Infrastructure.HealthChecks;
using SagaFlow.Infrastructure.Messaging;
using SagaFlow.InventoryService.Consumers;

var builder = Host.CreateApplicationBuilder(args);

// Configure MassTransit with RabbitMQ
builder.Services.AddMassTransitWithRabbitMq(builder.Configuration, cfg =>
{
    cfg.AddConsumer<ReserveInventoryConsumer>();
});

// Add Health Checks
builder.Services.AddInfrastructureHealthChecks(builder.Configuration);

var host = builder.Build();
host.Run();
