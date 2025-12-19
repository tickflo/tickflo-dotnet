using System.ComponentModel.DataAnnotations.Schema;

namespace Tickflo.Core.Entities
{
    public class TicketInventory
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int InventoryId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;

        [ForeignKey("TicketId")]
        public Ticket Ticket { get; set; }
        [ForeignKey("InventoryId")]
        public Inventory Inventory { get; set; }
    }
}
