using System.ComponentModel.DataAnnotations;
using DomCoreERP.Core.Entities.Core;

namespace DomCoreERP.Core.Entities;

public class Product : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string SKU { get; set; } = null!;
    public string? Description { get; set; }

    public decimal? PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }

    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }

    public bool IsActive { get; set; } = true;

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;

    // Navigation
    public ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();
    public ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
}