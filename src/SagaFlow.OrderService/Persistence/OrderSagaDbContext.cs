using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SagaFlow.OrderService.Sagas;

namespace SagaFlow.OrderService.Persistence;

public class OrderSagaDbContext(DbContextOptions<OrderSagaDbContext> options) : DbContext(options)
{
    public DbSet<OrderState> OrderStates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.HasDefaultSchema("saga");
        modelBuilder.ApplyConfiguration(new OrderStateMap());
    }
}

public class OrderStateMap : IEntityTypeConfiguration<OrderState>
{
    public void Configure(EntityTypeBuilder<OrderState> entity)
    {
        entity.ToTable("OrderStates");
        entity.HasKey(x => x.CorrelationId);
        
        entity.Property(x => x.CurrentState).HasMaxLength(64).IsRequired();
        entity.Property(x => x.CustomerId).HasMaxLength(128);
        entity.Property(x => x.TransactionId).HasMaxLength(128);
        entity.Property(x => x.ReservationId).HasMaxLength(128);
        entity.Property(x => x.FailureReason).HasMaxLength(512);
        entity.Property(x => x.OrderItemsJson).HasColumnType("jsonb");
        
        entity.HasIndex(x => x.CustomerId);
        entity.HasIndex(x => x.CreatedAt);
    }
}
