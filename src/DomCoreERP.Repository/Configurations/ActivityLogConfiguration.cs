using DomCoreERP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomCoreERP.Repository.Configurations;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> b)
    {
        b.HasKey(a => a.Id);

        b.Property(a => a.Action).HasMaxLength(100).IsRequired();
        b.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
        b.Property(a => a.Description).HasMaxLength(1000);
        b.Property(a => a.ActorRole).HasMaxLength(100).IsRequired();

        // ── FK: ActivityLog → ActorUser ──
        b.HasOne(a => a.ActorUser)
            .WithMany()
            .HasForeignKey(a => a.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── FK: ActivityLog → Company (nullable) ──
        // Company deletion shouldn't wipe audit history.
        // OnDelete(SetNull) means: if a company is ever hard-deleted, keep the log but lose the reference.
        b.HasOne(a => a.Company)
            .WithMany(c => c.ActivityLogs)
            .HasForeignKey(a => a.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Indexes ──
        // Recent activity per company (dashboard + list endpoints)
        b.HasIndex(a => new { a.CompanyId, a.Timestamp });
        // "what has this user been doing?"
        b.HasIndex(a => a.ActorUserId);
        // "show all activity on invoice X"
        b.HasIndex(a => new { a.EntityType, a.EntityId });
    }
}