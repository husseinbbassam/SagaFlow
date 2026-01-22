using MassTransit;

namespace SagaFlow.OrderService.Sagas;

public class OrderState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    
    public string CurrentState { get; set; } = string.Empty;
    
    public string? CustomerId { get; set; }
    
    public decimal TotalAmount { get; set; }
    
    public string? TransactionId { get; set; }
    
    public string? ReservationId { get; set; }
    
    public string? FailureReason { get; set; }
    
    public string? OrderItemsJson { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? CompletedAt { get; set; }
}
