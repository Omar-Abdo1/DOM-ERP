using DomCoreERP.Core.Entities.Core;
using DomCoreERP.Core.Enums;

namespace DomCoreERP.Core.Entities;

public class ActivityLog : BaseEntity
{
    public Guid ActorUserId { get; set; }
    public User ActorUser { get; set; } = null!;

    public UserRole ActorRole { get; set; }

    // Nullable: system-wide operations (company creation, login before company set)
    public Guid? CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Action { get; set; } = null!;      // "Created", "Issued", "Login", etc.
    public string EntityType { get; set; } = null!;  // "Invoice", "Product", "User", "Auth"
    public Guid? EntityId { get; set; }
    public string? Description { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}