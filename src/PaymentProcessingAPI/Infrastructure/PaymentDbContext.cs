using Microsoft.EntityFrameworkCore;
using PaymentProcessingAPI.Models.Entities;

namespace PaymentProcessingAPI.Infrastructure;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<Payment> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.TransactionId)
                  .IsUnique()
                  .HasDatabaseName("IX_Payment_TransactionId");

            entity.HasIndex(e => e.CustomerId)
                  .HasDatabaseName("IX_Payment_CustomerId");

            entity.HasIndex(e => e.Status)
                  .HasDatabaseName("IX_Payment_Status");

            entity.HasIndex(e => e.CreatedAt)
                  .HasDatabaseName("IX_Payment_CreatedAt");

            entity.Property(e => e.Amount)
                  .HasPrecision(18, 2);

            entity.Property(e => e.ProcessedAmount)
                  .HasPrecision(18, 2);

            entity.Property(e => e.ProcessingFee)
                  .HasPrecision(18, 2);

            entity.Property(e => e.GatewayFee)
                  .HasPrecision(18, 2);

            entity.Property(e => e.TotalFees)
                  .HasPrecision(18, 2);

            entity.Property(e => e.NetAmount)
                  .HasPrecision(18, 2);

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.UpdatedAt)
                  .HasDefaultValueSql("GETUTCDATE()")
                  .ValueGeneratedOnAddOrUpdate();
        });
    }
}