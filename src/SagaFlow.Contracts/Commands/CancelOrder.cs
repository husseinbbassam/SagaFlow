namespace SagaFlow.Contracts.Commands;

public record CancelOrder(
    Guid OrderId,
    string Reason,
    DateTime RequestedAt);
