using DomCoreERP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomCoreERP.Repository.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.HasKey(r => r.Id);

        b.Property(r => r.Token).HasMaxLength(500).IsRequired();
        b.Property(r => r.ReplacedByToken).HasMaxLength(500);
        b.Property(r => r.RevokedReason).HasMaxLength(200);
        
        b.HasOne(r=>r.User).
            WithMany(u=>u.RefreshTokens).
            HasForeignKey(r=>r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Lookup by token value (used on every /auth/refresh call)
        b.HasIndex(r => r.Token).IsUnique();

        // Cleanup jobs: WHERE ExpiresAt < now OR RevokedAt IS NOT NULL
        b.HasIndex(r => r.ExpiresAt);
        b.HasIndex(r => r.RevokedAt);
    }
}