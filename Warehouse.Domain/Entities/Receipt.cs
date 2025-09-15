namespace Warehouse.Domain.Entities
{
    public record Receipt
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public DateOnly Date { get; set; }
        public ICollection<ReceiptItem>? ReceiptItems { get; set; }
    }
}
