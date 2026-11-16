using DomCoreERP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomCoreERP.Repository.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> b)
    {
        b.HasKey(c => c.Id);

        b.Property(c => c.Name).HasMaxLength(200).IsRequired();
        b.Property(c => c.Description).HasMaxLength(1000);
        b.Property(c => c.Email).HasMaxLength(200);
        b.Property(c => c.Phone).HasMaxLength(30);
        b.Property(c => c.Address).HasMaxLength(500);
        b.Property(c => c.LogoUrl).HasMaxLength(500);

        // Company name is globally unique across the system
        b.HasIndex(c => c.Name).IsUnique();

        // Companies are filtered by status (dashboard "active vs inactive" counts)
        b.HasIndex(c => c.Status);
    }
}