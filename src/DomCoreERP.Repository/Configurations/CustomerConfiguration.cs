using DomCoreERP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomCoreERP.Repository.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.HasKey(c => c.Id);

        b.Property(c => c.Name).HasMaxLength(200).IsRequired();
        b.Property(c => c.Phone).HasMaxLength(30).IsRequired();
        b.Property(c => c.Email).HasMaxLength(200);
        b.Property(c => c.Address).HasMaxLength(500);
        b.Property(c => c.Notes).HasMaxLength(2000);
        b.Property(c => c.IsActive).HasDefaultValue(true);

        // ── FK: Customer → Company ──
        b.HasOne(c => c.Company)
            .WithMany(co => co.Customers)
            .HasForeignKey(c => c.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Filter indexes ──
        b.HasIndex(c => new { c.CompanyId, c.IsActive });
        b.HasIndex(c => new { c.CompanyId, c.Name });    // search by name
        b.HasIndex(c => new { c.CompanyId, c.Phone });   // search by phone
    }
}