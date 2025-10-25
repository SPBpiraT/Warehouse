using System.ComponentModel.DataAnnotations;

namespace Warehouse.Domain.Entities
{
    public record Receipt
    {
        public int Id { get; set; }
        [Display(Name = "Номер")]
        public int Number { get; set; }
        [Display(Name = "Дата")]
        public DateTime Date { get; set; }
        public IList<ReceiptItem>? ReceiptItems { get; set; }
    }
}
