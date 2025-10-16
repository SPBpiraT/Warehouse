namespace Warehouse.Domain.Entities
{
    public record Shipment
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public DateTime Date { get; set; }
        public int ClientId { get; set; }
        public Client? Client { get; set; }
        public bool IsSigned { get; set; }
        public IList<ShipmentItem>? ShipmentItems { get; set; }
    }
}
