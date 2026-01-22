namespace SagaFlow.Contracts.Events;

public record OrderCompleted(
    Guid OrderId,
    DateTime CompletedAt);
