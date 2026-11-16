using DomCoreERP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomCoreERP.Repository.Configurations;

public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> b)
    {
        b.HasKey(it => it.Id);

        // Snapshots — fixed at issuance, so size them generously
        b.Property(it => it.ProductName).HasMaxLength(200).IsRequired();
        b.Property(it => it.ProductCode).HasMaxLength(50);

        b.Property(it => it.UnitPrice).HasPrecision(18, 2);
        b.Property(it => it.LineTotal).HasPrecision(18, 2);

        // ── FK: InvoiceItem → Invoice ──
        // Note: this is already configured on InvoiceConfiguration (Cascade), but
        // it's harmless to be explicit here — it makes this file's behavior obvious.
        b.HasOne(it => it.Invoice)
            .WithMany(i => i.Items)
            .HasForeignKey(it => it.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── FK: InvoiceItem → Product (RESTRICT — never delete a sold product) ──
        b.HasOne(it => it.Product)
            .WithMany(p => p.InvoiceItems)
            .HasForeignKey(it => it.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Indexes ──
        b.HasIndex(it => it.InvoiceId);   // load items of an invoice
        b.HasIndex(it => it.ProductId);   // "how many times was this product sold?"
    }
}