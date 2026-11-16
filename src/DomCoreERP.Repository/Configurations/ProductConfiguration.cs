using DomCoreERP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomCoreERP.Repository.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.HasKey(p => p.Id);

        b.Property(p => p.Name).HasMaxLength(200).IsRequired();
        b.Property(p => p.SKU).HasMaxLength(50).IsRequired();
        b.Property(p => p.Description).HasMaxLength(1000);

        b.Property(p => p.PurchasePrice).HasPrecision(18, 2);
        b.Property(p => p.SellingPrice).HasPrecision(18, 2);

        b.Property(p => p.CurrentStock).HasDefaultValue(0m);
        b.Property(p => p.MinimumStock).HasDefaultValue(0m);

        b.Property(p => p.IsActive).HasDefaultValue(true);
        b.Property(p => p.RowVersion).IsRowVersion();

        // ── FK: Product → Company ──
        b.HasOne(p => p.Company)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Unique SKU per company ──
        b.HasIndex(p => new { p.CompanyId, p.SKU }).IsUnique();

        // ── Filter indexes ──
        b.HasIndex(p => new { p.CompanyId, p.IsActive });       // active product list
        b.HasIndex(p => new { p.CompanyId, p.Name });           // search by name
        b.HasIndex(p => new { p.CompanyId, p.CreatedAt });      // sort by newest
    }
}