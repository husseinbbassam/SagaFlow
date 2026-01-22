namespace SagaFlow.Contracts.Events;

public record StockUnavailable(
    Guid OrderId,
    List<string> UnavailableProducts,
    DateTime CheckedAt);
