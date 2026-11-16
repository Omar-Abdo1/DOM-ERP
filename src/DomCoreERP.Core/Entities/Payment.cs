using System.ComponentModel.DataAnnotations;
using DomCoreERP.Core.Entities.Core;
using DomCoreERP.Core.Enums;

namespace DomCoreERP.Core.Entities;

public class Payment : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }

    public Guid ReceivedBy { get; set; }             // UserId
    public User ReceivedByUser { get; set; } = null!;

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
}