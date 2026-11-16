using DomCoreERP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomCoreERP.Repository.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> b)
    {
        b.HasKey(i => i.Id);

        b.Property(i => i.InvoiceNumber).HasMaxLength(30).IsRequired();
        b.Property(i => i.Notes).HasMaxLength(2000);

        b.Property(i => i.Subtotal).HasPrecision(18, 2);
        b.Property(i => i.Discount).HasPrecision(18, 2);
        b.Property(i => i.Tax).HasPrecision(18, 2);
        b.Property(i => i.Total).HasPrecision(18, 2);
        b.Property(i => i.PaidAmount).HasPrecision(18, 2);
        b.Property(i => i.RemainingAmount).HasPrecision(18, 2);

        b.Property(i => i.RowVersion).IsRowVersion();

        // ── FKs ──
        b.HasOne(i => i.Company)
         .WithMany(c => c.Invoices)
         .HasForeignKey(i => i.CompanyId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(i => i.Customer)
         .WithMany(c => c.Invoices)
         .HasForeignKey(i => i.CustomerId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(i => i.CreatedByUser)
         .WithMany()
         .HasForeignKey(i => i.CreatedBy)
         .OnDelete(DeleteBehavior.Restrict);

        // ── The deliberate CASCADE: items belong to invoice ──
        b.HasMany(i => i.Items)
         .WithOne(it => it.Invoice)
         .HasForeignKey(it => it.InvoiceId)
         .OnDelete(DeleteBehavior.Cascade);

        // ── Payments block invoice deletion ──
        b.HasMany(i => i.Payments)
         .WithOne(p => p.Invoice)
         .HasForeignKey(p => p.InvoiceId)
         .OnDelete(DeleteBehavior.Restrict);

        // ── Invoice number unique per company ──
        b.HasIndex(i => new { i.CompanyId, i.InvoiceNumber }).IsUnique();

        // ── Filter indexes ──
        b.HasIndex(i => new { i.CompanyId, i.InvoiceDate });   // recent invoices (dashboard)
        b.HasIndex(i => new { i.CompanyId, i.Status });        // filter by status
        b.HasIndex(i => new { i.CompanyId, i.CustomerId });    // customer's invoices
    }
}