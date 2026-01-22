using MassTransit;
using SagaFlow.Contracts.Commands;
using SagaFlow.Contracts.Events;

namespace SagaFlow.InventoryService.Consumers;

public class ReserveInventoryConsumer(ILogger<ReserveInventoryConsumer> logger) : IConsumer<ReserveInventory>
{
    public async Task Consume(ConsumeContext<ReserveInventory> context)
    {
        logger.LogInformation("Reserving inventory for Order {OrderId}", context.Message.OrderId);

        // Simulate inventory check delay
        await Task.Delay(TimeSpan.FromSeconds(1));

        // Simulate stock availability (80% success rate)
        var isAvailable = Random.Shared.Next(100) < 80;

        if (isAvailable)
        {
            var reservationId = $"RES-{Guid.NewGuid():N}";
            
            logger.LogInformation("Inventory reserved for Order {OrderId}, Reservation: {ReservationId}", 
                context.Message.OrderId, 
                reservationId);

            await context.Publish(new InventoryReserved(
                context.Message.OrderId,
                reservationId,
                DateTime.UtcNow));
        }
        else
        {
            logger.LogWarning("Stock unavailable for Order {OrderId}", context.Message.OrderId);

            var unavailableProducts = context.Message.Items
                .Where(_ => Random.Shared.Next(100) < 50)
                .Select(i => i.ProductId)
                .ToList();

            if (unavailableProducts.Count == 0)
            {
                unavailableProducts.Add("PRODUCT-UNKNOWN");
            }

            await context.Publish(new StockUnavailable(
                context.Message.OrderId,
                unavailableProducts,
                DateTime.UtcNow));
        }
    }
}
