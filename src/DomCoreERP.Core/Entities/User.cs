using DomCoreERP.Core.Entities.Core;
using DomCoreERP.Core.Enums;
using Microsoft.AspNetCore.Identity;

namespace DomCoreERP.Core.Entities;

public class User : IdentityUser<Guid>, IEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Guid? CompanyId { get; set; }
    public Company? Company { get; set; }

    public string FullName { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}   