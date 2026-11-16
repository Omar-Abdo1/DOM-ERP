using DomCoreERP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomCoreERP.Repository.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        
        b.Property(u => u.FullName).HasMaxLength(200).IsRequired();
        b.Property(u => u.IsActive).HasDefaultValue(true);

        // ── FK: User → Company (nullable for SuperAdmin) ──
        b.HasOne(u => u.Company)
            .WithMany(c => c.Users)
            .HasForeignKey(u => u.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── FK: User → RefreshTokens (cascade — tokens are ephemeral) ──
        b.HasMany(u => u.RefreshTokens)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Indexes ──
        b.HasIndex(u => u.CompanyId);            // filter users by company
        b.HasIndex(u => new { u.CompanyId, u.IsActive });  // active users in a company
    }
    
}