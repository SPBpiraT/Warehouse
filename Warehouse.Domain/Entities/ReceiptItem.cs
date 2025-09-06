namespace Warehouse.Domain.Entities
{
    public record ReceiptItem
    {
        public int Id { get; set; }
        public int ReceiptId { get; set; }
        public int ResourceId { get; set; }
        public int UnitId { get; set; }
        public Resource? Resource { get; set; }
        public Unit? Unit { get; set; }
        public ulong Quantity { get; set; }
    }
}
