using DomCoreERP.Core.Entities.Core;
using DomCoreERP.Core.Enums;

namespace DomCoreERP.Core.Entities;

public class InventoryMovement : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public InventoryMovementType MovementType { get; set; }
    public decimal Quantity { get; set; }            // signed: +in / -out
    public decimal PreviousStock { get; set; }
    public decimal NewStock { get; set; }

    public string? ReferenceType { get; set; }       // "Invoice", "ManualAdjustment", "OpeningStock"
    public Guid? ReferenceId { get; set; }           // e.g. Invoice.Id

    public string? Notes { get; set; }

    public Guid CreatedBy { get; set; }              // UserId
    public User CreatedByUser { get; set; } = null!;
}