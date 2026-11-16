namespace DomCoreERP.Core.Enums;

public enum InventoryMovementType
{
    Opening = 1,           // initial stock on product creation
    Sale = 2,              // stock out from issued invoice
    Adjustment = 3,        // manual admin adjustment
    CancellationRestore = 4 // stock back from cancelled invoice
}