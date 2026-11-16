using DomCoreERP.Core.Entities.Core;
using DomCoreERP.Core.Enums;

namespace DomCoreERP.Core.Entities;

public class Company : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? LogoUrl { get; set; }
    public CompanyStatus Status { get; set; } = CompanyStatus.Active;

    // Navigation
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();
    public ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
}