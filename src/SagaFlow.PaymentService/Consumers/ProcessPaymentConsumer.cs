using MassTransit;
using SagaFlow.Contracts.Commands;
using SagaFlow.Contracts.Events;

namespace SagaFlow.PaymentService.Consumers;

public class ProcessPaymentConsumer(ILogger<ProcessPaymentConsumer> logger) : IConsumer<ProcessPayment>
{
    public async Task Consume(ConsumeContext<ProcessPayment> context)
    {
        logger.LogInformation("Processing payment for Order {OrderId}, Amount: {Amount}", 
            context.Message.OrderId, 
            context.Message.Amount);

        // Simulate payment processing delay
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Simulate payment success/failure (90% success rate)
        var isSuccess = Random.Shared.Next(100) < 90;

        if (isSuccess)
        {
            var transactionId = $"TXN-{Guid.NewGuid():N}";
            
            logger.LogInformation("Payment approved for Order {OrderId}, Transaction: {TransactionId}", 
                context.Message.OrderId, 
                transactionId);

            await context.Publish(new PaymentApproved(
                context.Message.OrderId,
                transactionId,
                context.Message.Amount,
                DateTime.UtcNow));
        }
        else
        {
            logger.LogWarning("Payment failed for Order {OrderId}", context.Message.OrderId);

            await context.Publish(new PaymentFailed(
                context.Message.OrderId,
                "Insufficient funds or payment declined",
                DateTime.UtcNow));
        }
    }
}
