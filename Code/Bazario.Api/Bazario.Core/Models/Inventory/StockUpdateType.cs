namespace Bazario.Core.Models.Inventory
{
    /// <summary>
    /// Stock update types
    /// </summary>
    public enum StockUpdateType
    {
        Purchase,        // Incoming stock
        Sale,           // Outgoing stock
        Adjustment,     // Manual adjustment
        Return,         // Customer return
        Damage,         // Damaged goods
        Transfer,       // Store transfer
        Correction      // Inventory correction
    }
}
