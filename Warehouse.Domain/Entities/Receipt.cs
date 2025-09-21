using System.ComponentModel.DataAnnotations;

namespace Warehouse.Domain.Entities
{
    public record Receipt
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public DateTime Date { get; set; }
        public IList<ReceiptItem>? ReceiptItems { get; set; }
    }
}
