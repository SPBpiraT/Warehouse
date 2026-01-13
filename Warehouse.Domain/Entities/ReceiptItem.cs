using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Warehouse.Domain.Interfaces;

namespace Warehouse.Domain.Entities
{
    public class ReceiptItem : IOperationItem
    {
        public int Id { get; set; }
        public int ReceiptId { get; set; }
        public int ResourceId { get; set; }
        public int UnitId { get; set; }
        [ValidateNever]
        public Resource Resource { get; set; }
        [ValidateNever]
        public Unit Unit { get; set; }
        public ulong Quantity { get; set; }
    }
}
