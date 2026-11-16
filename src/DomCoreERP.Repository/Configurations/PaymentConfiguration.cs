using DomCoreERP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomCoreERP.Repository.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.HasKey(p => p.Id);

        b.Property(p => p.Amount).HasPrecision(18, 2);
        b.Property(p => p.Reference).HasMaxLength(100);
        b.Property(p => p.Notes).HasMaxLength(2000);
        b.Property(p => p.PaymentMethod).HasMaxLength(50).IsRequired();

        b.Property(p => p.RowVersion).IsRowVersion();

        // ── FKs ──
        b.HasOne(p => p.Company)
            .WithMany(c => c.Payments)
            .HasForeignKey(p => p.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(p => p.Invoice)
            .WithMany(i => i.Payments)
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(p => p.ReceivedByUser)
            .WithMany()
            .HasForeignKey(p => p.ReceivedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Indexes ──
        b.HasIndex(p => new { p.CompanyId, p.PaymentDate });    // recent payments list
        b.HasIndex(p => p.InvoiceId);                            // payments for invoice
    }
}