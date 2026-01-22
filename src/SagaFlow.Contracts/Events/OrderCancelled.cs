namespace SagaFlow.Contracts.Events;

public record OrderCancelled(
    Guid OrderId,
    string Reason,
    DateTime CancelledAt);
