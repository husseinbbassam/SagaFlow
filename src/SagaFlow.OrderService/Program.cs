using MassTransit;
using Microsoft.EntityFrameworkCore;
using SagaFlow.Infrastructure.HealthChecks;
using SagaFlow.Infrastructure.Messaging;
using SagaFlow.OrderService.Persistence;
using SagaFlow.OrderService.Sagas;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Configure PostgreSQL for Saga persistence
builder.Services.AddDbContext<OrderSagaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("SagaDb")));

// Configure MassTransit with RabbitMQ and Saga
builder.Services.AddMassTransitWithRabbitMq(builder.Configuration, cfg =>
{
    cfg.AddSagaStateMachine<OrderStateMachine, OrderState>()
        .EntityFrameworkRepository(r =>
        {
            r.ExistingDbContext<OrderSagaDbContext>();
            r.UsePostgres();
        });
});

// Add Health Checks
builder.Services.AddInfrastructureHealthChecks(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
