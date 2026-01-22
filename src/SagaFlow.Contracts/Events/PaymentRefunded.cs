namespace SagaFlow.Contracts.Events;

public record PaymentRefunded(
    Guid OrderId,
    string TransactionId,
    decimal Amount,
    DateTime RefundedAt);
