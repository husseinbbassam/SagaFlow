using SagaFlow.Infrastructure.HealthChecks;
using SagaFlow.Infrastructure.Messaging;
using SagaFlow.PaymentService.Consumers;

var builder = Host.CreateApplicationBuilder(args);

// Configure MassTransit with RabbitMQ
builder.Services.AddMassTransitWithRabbitMq(builder.Configuration, cfg =>
{
    cfg.AddConsumer<ProcessPaymentConsumer>();
    cfg.AddConsumer<RefundPaymentConsumer>();
});

// Add Health Checks
builder.Services.AddInfrastructureHealthChecks(builder.Configuration);

var host = builder.Build();
host.Run();
