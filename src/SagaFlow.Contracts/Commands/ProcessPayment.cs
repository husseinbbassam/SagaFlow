namespace SagaFlow.Contracts.Commands;

public record ProcessPayment(
    Guid OrderId,
    string CustomerId,
    decimal Amount,
    DateTime RequestedAt);
