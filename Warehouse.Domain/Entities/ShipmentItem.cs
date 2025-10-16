namespace Warehouse.Domain.Entities
{
    public record ShipmentItem
    {
        public int Id { get; set; }
        public int ShipmentId { get; set; }
        public int ResourceId { get; set; }
        public int UnitId { get; set; }
        public Resource? Resource { get; set; }
        public Unit? Unit { get; set; }
        public ulong Quantity { get; set; }
    }
}
