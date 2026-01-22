namespace SagaFlow.Contracts.Events;

public record PaymentApproved(
    Guid OrderId,
    string TransactionId,
    decimal Amount,
    DateTime ApprovedAt);
