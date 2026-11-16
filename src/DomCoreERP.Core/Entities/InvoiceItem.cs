using DomCoreERP.Core.Entities.Core;

namespace DomCoreERP.Core.Entities;

public class InvoiceItem : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    // Snapshots — immutable after invoice is issued
    public string ProductName { get; set; } = null!;
    public string? ProductCode { get; set; }
    public decimal UnitPrice { get; set; }

    public decimal Quantity { get; set; }
    public decimal LineTotal { get; set; }
}