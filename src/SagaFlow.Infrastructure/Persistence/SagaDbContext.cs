using Microsoft.EntityFrameworkCore;

namespace SagaFlow.Infrastructure.Persistence;

public class SagaDbContext(DbContextOptions<SagaDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.HasDefaultSchema("saga");
    }
}
