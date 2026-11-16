using System.ComponentModel.DataAnnotations;
using DomCoreERP.Core.Entities.Core;
using DomCoreERP.Core.Enums;

namespace DomCoreERP.Core.Entities;

public class Invoice : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public string InvoiceNumber { get; set; } = null!;

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public DateTime InvoiceDate { get; set; }

    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }

    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public string? Notes { get; set; }

    public Guid CreatedBy { get; set; }              // UserId
    public User CreatedByUser { get; set; } = null!;

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;

    // Navigation
    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}