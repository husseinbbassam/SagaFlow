namespace SagaFlow.Contracts.Events;

public record PaymentFailed(
    Guid OrderId,
    string Reason,
    DateTime FailedAt);
