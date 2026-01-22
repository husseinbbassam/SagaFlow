namespace SagaFlow.Contracts.Commands;

public record RefundPayment(
    Guid OrderId,
    string TransactionId,
    decimal Amount,
    string Reason,
    DateTime RequestedAt);
