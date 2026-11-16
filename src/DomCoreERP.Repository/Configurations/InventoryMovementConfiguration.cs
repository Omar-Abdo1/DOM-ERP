using DomCoreERP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomCoreERP.Repository.Configurations;

public class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> b)
    {
        b.HasKey(m => m.Id);


        b.Property(m => m.ReferenceType).HasMaxLength(50);
        b.Property(m => m.Notes).HasMaxLength(500);
        b.Property(m => m.MovementType).HasMaxLength(50).IsRequired();

        // ── FKs ──
        b.HasOne(m => m.Company)
            .WithMany(c => c.InventoryMovements)
            .HasForeignKey(m => m.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(m => m.Product)
            .WithMany(p => p.InventoryMovements)
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(m => m.CreatedByUser)
            .WithMany()
            .HasForeignKey(m => m.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Indexes ──
        b.HasIndex(m => new { m.CompanyId, m.CreatedAt });   // company's recent movements
        b.HasIndex(m => new { m.ProductId, m.CreatedAt });   // product's history
        // "find all movements from invoice X"
        b.HasIndex(m => new { m.ReferenceType, m.ReferenceId });
    }
}