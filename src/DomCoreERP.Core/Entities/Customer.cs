using DomCoreERP.Core.Entities.Core;

namespace DomCoreERP.Core.Entities;

public class Customer : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}