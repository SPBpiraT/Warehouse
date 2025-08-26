namespace Warehouse.Domain.Entities
{
    public record Balance
    {
        public int Id { get; set; }
        public int ResourceId { get; set; }
        public int UnitId { get; set; }
        public ulong Quantity { get; set; }
        public Resource? Resource { get; set; }
        public Unit? Unit { get; set; }
    }
}
